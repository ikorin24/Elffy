#nullable enable
using Cysharp.Threading.Tasks;
using Elffy.Components.Implementation;
using Elffy.Effective;
using Elffy.Effective.Unsafes;
using Elffy.Mathematics;
using Elffy.Features;
using Elffy.Graphics.OpenGL;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Elffy.Components
{
    public class Skeleton : ISingleOwnerComponent
    {
        private SingleOwnerComponentCore _core;             // Mutable object, Don't change into readonly
        private FloatDataTextureCore _boneTranslationData;  // Mutable object, Don't change into readonly

        private UnsafeRawArray<Matrix4> _posMatrices;      // Position matrix of each model in model local coordinate (It's read-only after loaded once.)
        private UnsafeRawArray<Matrix4> _posInvMatrices;   // Inverse of _posMatrices (It's read-only after loaded once.)
        private UnsafeRawArray<Matrix4> _translations;     // Translation matrix of each bone, which coordinate is bone local. (not model local)
        private UnsafeRawArray<Matrix4> _matrices;         // Buffer to send matrix to data texture
        private UnsafeRawArray<BoneInternal> _tree;        // Bone tree to walk around all bones
        private bool _disposed;

        private bool IsBoneLoaded => !_boneTranslationData.TextureObject.IsEmpty;

        /// <inheritdoc/>
        public ComponentOwner? Owner => _core.Owner;

        /// <inheritdoc/>
        public bool AutoDisposeOnDetached => _core.AutoDisposeOnDetached;

        /// <summary>Get bone count</summary>
        public int BoneCount => _tree.Length;

        /// <summary>Get translation matrices of each bone as readonly.</summary>
        /// <remarks>If you want to edit matrices, use <see cref="StartTranslation"/> method.</remarks>
        public ReadOnlySpan<Matrix4> Translations => _translations.AsSpan();

        /// <summary>Get <see cref="TextureObject"/> of bone translation matrices. (This is Texture1D)</summary>
        public TextureObject TranslationData => _boneTranslationData.TextureObject;

        public Skeleton()
        {
        }

        ~Skeleton() => Dispose(false);

        /// <summary>Load bone data</summary>
        /// <param name="bones">bone data</param>
        public void Load(ReadOnlySpan<Bone> bones)
        {
            if(IsBoneLoaded) { throw new InvalidOperationException("Already loaded"); }
            InitializeSkeletonData(this, bones);
            _boneTranslationData.Load(_matrices!.AsSpan().MarshalCast<Matrix4, Color4>());
            ContextAssociatedMemorySafety.Register(this, Engine.CurrentContext!);
        }

        public UniTask LoadAsync<TBoneSpan>(TBoneSpan bones, FrameTimingPointList timingPoints, CancellationToken cancellationToken = default) where TBoneSpan : IReadOnlySpan<Bone>
        {
            return LoadAsync(bones, timingPoints, FrameTiming.Update, cancellationToken);
        }

        public async UniTask LoadAsync<TBoneSpan>(TBoneSpan bones, FrameTimingPointList timingPoints, FrameTiming timing,
                                                  CancellationToken cancellationToken = default) where TBoneSpan : IReadOnlySpan<Bone>
        {
            if(IsBoneLoaded) { throw new InvalidOperationException("Already loaded"); }
            if(timingPoints is null) { throw new ArgumentNullException(nameof(timingPoints)); }
            timing.ThrowArgExceptionIfNotSpecified(nameof(timing));

            cancellationToken.ThrowIfCancellationRequested();
            await UniTask.SwitchToThreadPool();
            cancellationToken.ThrowIfCancellationRequested();
            try {
                InitializeSkeletonData(this, bones.AsReadOnlySpan());
                await timingPoints.TimingOf(timing).Next(cancellationToken);
                if(IsBoneLoaded) { throw new InvalidOperationException("Already loaded"); }
                _boneTranslationData.Load(_matrices!.AsSpan().MarshalCast<Matrix4, Color4>());
            }
            catch {
                _posMatrices.Dispose();
                _posInvMatrices.Dispose();
                _tree.Dispose();
                _matrices.Dispose();
                _translations.Dispose();
                throw;
            }

            ContextAssociatedMemorySafety.Register(this, Engine.CurrentContext!);
            return;
        }

        /// <inheritdoc/>
        void IComponent.OnAttached(ComponentOwner owner) => _core.OnAttached(owner, this);

        /// <inheritdoc/>
        void IComponent.OnDetached(ComponentOwner owner) => _core.OnDetached(owner, this);

        /// <summary>Get handler to edit translation matrices. (Call <see cref="SkeletonHandler.Dispose"/> to end editing.)</summary>
        /// <returns>handler to edit translation matrices.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe SkeletonHandler StartTranslation()
        {
            if(!IsBoneLoaded) {
                Throw();
                [DoesNotReturn] static void Throw() => throw new InvalidOperationException("Bones are not loaded.");
            }
            AssertValid();
            return new SkeletonHandler(this, (Matrix4*)_posMatrices!.Ptr, (Matrix4*)_translations!.Ptr, (BoneInternal*)_tree!.Ptr, _translations.Length);
        }

        /// <summary>Send matrices to opengl. This method is called from <see cref="SkeletonHandler"/></summary>
        internal unsafe void UpdateTranslations()
        {
            if(!IsBoneLoaded) { return; }
            AssertValid();

            var matrices = (Matrix4*)_matrices!.Ptr;
            var tree = (BoneInternal*)_tree!.Ptr;
            var translations = (Matrix4*)_translations!.Ptr;
            var pos = (Matrix4*)_posMatrices!.Ptr;
            var posInv = (Matrix4*)_posInvMatrices!.Ptr;

            // Calc matrices in model local coordinate.
            // You can walk around all bones in the tree by following to "b->Next".
            // The order is DFS (Depth First Search).
            // Therefore, matrices[b->Parent->ID] has been calculated when you will calculate matrices[b->ID].
            for(BoneInternal* b = tree; b != null; b = b->Next) {
                var i = b->ID;
                matrices[i] = pos[i] * translations[i] * posInv[i];
                if(b->Parent != null) {
                    matrices[i] = matrices[b->Parent->ID] * matrices[i];
                }
            }

            _boneTranslationData.Update(_matrices.AsSpan().MarshalCast<Matrix4, Color4>(), 0);
        }

        /// <summary>Dispose resources</summary>
        void IDisposable.Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if(_disposed) { return; }

            _posMatrices.Dispose();
            _posInvMatrices.Dispose();
            _matrices.Dispose();
            _translations.Dispose();
            _tree.Dispose();
            if(disposing) {
                _boneTranslationData.Dispose();
            }
            else {
                ContextAssociatedMemorySafety.OnFinalized(this);
            }
            _disposed = true;
        }

        [Conditional("DEBUG")]
        private void AssertValid()
        {
            Debug.Assert(_tree.IsEmpty == false);
            Debug.Assert(_matrices.IsEmpty == false);
            Debug.Assert(_translations.IsEmpty == false);
            Debug.Assert(_posMatrices.IsEmpty == false);
            Debug.Assert(_posInvMatrices.IsEmpty == false);
            Debug.Assert(_tree.Length <= _matrices.Length &&        // _matrices.Length is rounded up to power of two.
                         _tree.Length == _translations.Length &&
                         _tree.Length == _posMatrices.Length &&
                         _tree.Length == _posInvMatrices.Length);
        }

        private static void InitializeSkeletonData(Skeleton skeleton, ReadOnlySpan<Bone> bones)
        {
            // This method can run in any thread.


            CreateBoneTree(bones, out skeleton._posMatrices, out skeleton._posInvMatrices, out skeleton._tree);

            // Round up pixel count of bone matrix buffer on data texture to 2^n.
            var pixelCount = MathTool.RoundUpToPowerOfTwo(bones.Length * Matrix4.SizeInBytes / Vector4.SizeInBytes);
            var bufLen = pixelCount * Vector4.SizeInBytes / Matrix4.SizeInBytes;

            // Load matrices as identity
            var matrices = new UnsafeRawArray<Matrix4>(bufLen, false);
            matrices.AsSpan().Fill(Matrix4.Identity);
            skeleton._matrices = matrices;

            // Initialize translations as identity
            var translations = new UnsafeRawArray<Matrix4>(bones.Length, false);
            translations.AsSpan().Fill(Matrix4.Identity);
            skeleton._translations = translations;
        }

        private unsafe static void CreateBoneTree(ReadOnlySpan<Bone> bones,
                                                  out UnsafeRawArray<Matrix4> posMatrices,
                                                  out UnsafeRawArray<Matrix4> posInvMatrices,
                                                  out UnsafeRawArray<BoneInternal> boneTree)
        {
            // This method is O(NM). (in the worst case)
            // N is bones.Length, M is depth of bone tree

            boneTree = default;
            posMatrices = default;
            posInvMatrices = default;
            try {
                posMatrices = new UnsafeRawArray<Matrix4>(bones.Length, true);
                posInvMatrices = new UnsafeRawArray<Matrix4>(bones.Length, true);

                var pos = (Matrix4*)posMatrices.Ptr;
                var posInv = (Matrix4*)posInvMatrices.Ptr;

                boneTree = new UnsafeRawArray<BoneInternal>(bones.Length, true);
                var tree = (BoneInternal*)boneTree.Ptr;

                // Initialize tree of 'ID' and 'Parent'.
                // ('Next' is not set yet.)
                for(int i = 0; i < bones.Length; i++) {
                    tree[i].ID = i;                      // tree[i].ID is index in the tree.
                    if(bones[i].ParentBone != null) {
                        tree[i].Parent = &tree[(int)bones[i].ParentBone!];
                    }
                    //bones[i].Position.ToTranslationMatrix4(out pos[i]);
                    pos[i] = bones[i].Transform;
                    pos[i].Inverted(out posInv[i]);                         // Calc inverse of pos matrix in advance.
                }

                // 'childrenBuf[i]' means IDs of children of tree[i].
                // It is like 'List<int>[]'
                var childrenBuf = new UnsafeRawArray<UnsafeRawList<int>>(boneTree.Length, zeroFill: true);

                try {
                    // Set children
                    for(int i = 0; i < childrenBuf.Length; i++) {
                        if(tree[i].Parent != null) {
                            var parentID = tree[i].Parent->ID;
                            childrenBuf[parentID] = new UnsafeRawList<int>();
                            childrenBuf[parentID].Add(i);
                        }
                    }

                    // Initialize tree of 'Next'
                    // Create tree to walk around all bones in the way of DFS (depth first search).
                    for(int i = 0; i < boneTree.Length; i++) {
                        if(childrenBuf[i].IsNull == false && childrenBuf[i].Count > 0) {  // If tree[i] has any children
                            var firstChildID = childrenBuf[i][0];
                            tree[i].Next = &tree[firstChildID];     // tree[i].Next is first child of itself.
                        }
                        else {                                      // When tree[i] has no children
                            ref var target = ref tree[i];           // For the first time, target is tree[i].
                            while(true) {
                                if(target.Parent == null) {         // If target is root
                                    tree[i].Next = null;            // tree[i].Next is null
                                    break;
                                }
                                ref var siblings = ref childrenBuf[target.Parent->ID];  // Get siblings of target.
                                var num = siblings.IndexOf(target.ID);                  // Get number of target in the siblings.
                                if(num < siblings.Count - 1) {                          // That means tree[i] is not the last child in its siblings.
                                    tree[i].Next = &tree[siblings[num + 1]];            // tree[i].Next is next sibling of itself.
                                    break;
                                }
                                // When tree[i] is the last child in its siblings.
                                // If target is not root, change target to its parent
                                // and search recursively.
                                target = ref *(target.Parent);
                            }
                        }
                    }
                }
                finally {
                    for(int i = 0; i < childrenBuf.Length; i++) {
                        childrenBuf[i].Dispose();
                    }
                    childrenBuf.Dispose();
                }
            }
            catch {
                posMatrices.Dispose();
                posInvMatrices.Dispose();
                boneTree.Dispose();
                throw;
            }
        }
    }

    [DebuggerDisplay("{DebugView}")]
    internal unsafe struct BoneInternal
    {
        public int ID;
        public BoneInternal* Parent;
        public BoneInternal* Next;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly string DebugView
        {
            get
            {
                return $"ID: {ID}, Parent: {(Parent == null ? "null" : Parent->ID.ToString())}, Next: {(Next == null ? "null" : Next->ID.ToString())}";
            }
        }

        public override string ToString() => DebugView;
    }

    [DebuggerDisplay("{DebugView}")]
    public readonly struct Bone : IEquatable<Bone>
    {
        /// <summary>Get parent bone. (null if root bone)</summary>
        public readonly int? ParentBone;

        /// <summary>The transformation matrix from the model coordinate system to the bone coordinate system.</summary>
        /// <remarks>
        /// To get length of the bone, multiply the matrix to (1, 0, 0).<para/>
        /// To get position of the bone, multiply the matrix to (0, 0, 0).<para/>
        /// </remarks>
        public readonly Matrix4 Transform;

        /// <summary>Get direction of the bone. <see cref="Direction"/>.Length means length of the bone.</summary>
        public Vector3 Direction => (Transform * new Vector4(1f, 0, 0, 1f)).Xyz;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebugView
        {
            get
            {
                return $"Position: {this.GetPosition()}, Parent: {(ParentBone == null ? "null" : ParentBone.Value.ToString())}, Direction: {Direction}";
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Bone(int? parentBone, in Matrix4 transform)
        {
            ParentBone = parentBone;
            Transform = transform;
        }

        public override bool Equals(object? obj) => obj is Bone bone && Equals(bone);

        public bool Equals(Bone other) => (ParentBone == other.ParentBone) && (Transform == other.Transform);

        public override int GetHashCode() => HashCode.Combine(ParentBone, Transform);

        public override string ToString() => DebugView;
    }

    public static class BoneExtension
    {
        /// <summary>Get position of the bone in model coordinage system.</summary>
        /// <param name="bone">the bone</param>
        /// <returns>position of the bone</returns>
        public static ref readonly Vector3 GetPosition(this in Bone bone)
        {
            //(bone.Transform * new Vector4(0f, 0, 0, 1f)).Xyz;
            return ref UnsafeEx.As<Vector4, Vector3>(in Matrix4.Col3(bone.Transform));
        }
    }
}
