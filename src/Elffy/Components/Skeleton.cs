#nullable enable
using Cysharp.Text;
using Elffy.Core;
using Elffy.Effective;
using Elffy.Effective.Unsafes;
using Elffy.Mathematics;
using Elffy.OpenGL;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnmanageUtility;

namespace Elffy.Components
{
    public class Skeleton : ISingleOwnerComponent, IDisposable
    {
        private SingleOwnerComponentCore<Skeleton> _core = new SingleOwnerComponentCore<Skeleton>(true);    // Mutable object, Don't change into readonly
        private FloatDataTextureImpl _boneTranslationData = new FloatDataTextureImpl();                     // Mutable object, Don't change into readonly

        private UnmanagedArray<Matrix4>? _posMatrices;      // Position matrix of each model in model local coordinate (It's read-only after loaded once.)
        private UnmanagedArray<Matrix4>? _posInvMatrices;   // Inverse of _posMatrices (It's read-only after loaded once.)
        private UnmanagedArray<Matrix4>? _translations;     // Translation matrix of each bone, which coordinate is bone local. (not model local)
        private UnmanagedArray<Matrix4>? _matrices;         // Buffer to send matrix to data texture
        private UnmanagedArray<BoneInternal>? _tree;        // Bone tree to walk around all bones
        private bool _disposed;

        private bool IsBoneLoaded => !_boneTranslationData.TextureObject.IsEmpty;

        /// <inheritdoc/>
        public ComponentOwner? Owner => _core.Owner;

        /// <inheritdoc/>
        public bool AutoDisposeOnDetached => _core.AutoDisposeOnDetached;

        /// <summary>Get bone count</summary>
        public int BoneCount => _tree?.Length ?? 0;

        /// <summary>Get translation matrices of each bone as readonly.</summary>
        /// <remarks>If you want to edit matrices, use <see cref="StartTranslation"/> method.</remarks>
        public ReadOnlySpan<Matrix4> Translations => (_translations is not null) ? _translations.AsSpan() : default;

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

            CreateBoneTree(bones, out _posMatrices, out _posInvMatrices, out _tree);

            // Round up pixel count of bone matrix buffer on data texture to 2^n.
            var pixelCount = MathTool.RoundUpToPowerOfTwo(bones.Length * Matrix4.SizeInBytes / Vector4.SizeInBytes);
            var bufLen = pixelCount * Vector4.SizeInBytes / Matrix4.SizeInBytes;

            // Load matrices as identity
            _matrices = new UnmanagedArray<Matrix4>(bufLen, fill: Matrix4.Identity);
            _boneTranslationData.Load(_matrices.AsSpan().MarshalCast<Matrix4, Color4>());

            // Initialize translations as identity
            _translations = new UnmanagedArray<Matrix4>(bones.Length, fill: Matrix4.Identity);

            ContextAssociatedMemorySafety.Register(this, Engine.CurrentContext!);
        }

        /// <inheritdoc/>
        void IComponent.OnAttached(ComponentOwner owner) => _core.OnAttached(owner);

        /// <inheritdoc/>
        void IComponent.OnDetached(ComponentOwner owner) => _core.OnDetachedForDisposable(owner, this);


        /// <summary>Get handler to edit translation matrices. (Call <see cref="SkeletonHandler.Dispose"/> to end editing.)</summary>
        /// <returns>handler to edit translation matrices.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe SkeletonHandler StartTranslation()
        {
            return (_translations is not null) ? new SkeletonHandler(this, _translations.Ptr, _translations.Length)
                                               : new SkeletonHandler(this, IntPtr.Zero, 0);
        }

        /// <summary>Send matrices to opengl. This method is called from <see cref="SkeletonHandler"/></summary>
        internal unsafe void UpdateTranslations()
        {
            if(!IsBoneLoaded) { return; }
            Debug.Assert(_tree is not null);
            Debug.Assert(_matrices is not null);
            Debug.Assert(_translations is not null);
            Debug.Assert(_posMatrices is not null);
            Debug.Assert(_posInvMatrices is not null);
            Debug.Assert(_tree.Length <= _matrices.Length &&        // _matrices.Length is rounded up to power of two.
                         _tree.Length == _translations.Length &&
                         _tree.Length == _posMatrices.Length &&
                         _tree.Length == _posInvMatrices.Length);

            var matrices = (Matrix4*)_matrices.Ptr;
            var tree = (BoneInternal*)_tree.Ptr;
            var translations = (Matrix4*)_translations.Ptr;
            var pos = (Matrix4*)_posMatrices.Ptr;
            var posInv = (Matrix4*)_posInvMatrices.Ptr;

            // Calc matrices in model local coordinate.
            // You can walk around all bones in the tree by following to "b->Next".
            // The order is DFS (Depth First Search).
            // Therefore, matrices[b->Parent->ID] has been calculated when you will calculate matrices[b->ID].
            for(BoneInternal* b = tree; b != null; b = b->Next) {
                matrices[b->ID] = pos[b->ID] * translations[b->ID] * posInv[b->ID];
                if(b->Parent != null) {
                    matrices[b->ID] = matrices[b->Parent->ID] * matrices[b->ID];
                }
            }

            _boneTranslationData.Update(_matrices.AsSpan().MarshalCast<Matrix4, Color4>(), 0);
        }

        /// <summary>Dispose resources</summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if(_disposed) { return; }

            if(disposing) {
                _boneTranslationData.Dispose();

                _posMatrices?.Dispose();
                _posInvMatrices?.Dispose();
                _matrices?.Dispose();
                _translations?.Dispose();
                _tree?.Dispose();

                _posMatrices = null;
                _posInvMatrices = null;
                _matrices = null;
                _translations = null;
                _tree = null;
            }
            else {
                ContextAssociatedMemorySafety.OnFinalized(this);
            }
            _disposed = true;
        }

        private unsafe static void CreateBoneTree(ReadOnlySpan<Bone> bones,
                                                  out UnmanagedArray<Matrix4> posMatrices,
                                                  out UnmanagedArray<Matrix4> posInvMatrices,
                                                  out UnmanagedArray<BoneInternal> boneTree)
        {
            // This method is O(NM). (in the worst case)
            // N is bones.Length, M is depth of bone tree

            boneTree = null!;
            posMatrices = null!;
            posInvMatrices = null!;
            try {
                posMatrices = new UnmanagedArray<Matrix4>(bones.Length);
                posInvMatrices = new UnmanagedArray<Matrix4>(bones.Length);

                var pos = (Matrix4*)posMatrices.Ptr;
                var posInv = (Matrix4*)posInvMatrices.Ptr;

                boneTree = new UnmanagedArray<BoneInternal>(bones.Length);
                var tree = (BoneInternal*)boneTree.Ptr;

                // Initialize tree of 'ID' and 'Parent'.
                // ('Next' is not set yet.)
                for(int i = 0; i < bones.Length; i++) {
                    tree[i].ID = i;                      // tree[i].ID is index in the tree.
                    if(bones[i].ParentBone != null) {
                        tree[i].Parent = &tree[(int)bones[i].ParentBone!];
                    }
                    bones[i].Position.ToTranslationMatrix4(out pos[i]);
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
                            childrenBuf[parentID].Add(i);
                        }
                    }

                    // Initialize tree of 'Next'
                    // Create tree to walk around all bones in the way of DFS (depth first search).
                    for(int i = 0; i < boneTree.Length; i++) {
                        if(childrenBuf[i].Count > 0) {              // If tree[i] has any children
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
                catch {
                    for(int i = 0; i < childrenBuf.Length; i++) {
                        childrenBuf[i].Dispose();
                    }
                    childrenBuf.Dispose();
                    throw;
                }
            }
            catch {
                posMatrices?.Dispose();
                posInvMatrices?.Dispose();
                boneTree?.Dispose();
                throw;
            }
        }
    }

    /// <summary>Translation matrices handler of <see cref="Skeleton"/></summary>
    public readonly unsafe struct SkeletonHandler : IDisposable
    {
        private readonly Skeleton _skeleton;
        private readonly IntPtr _ptr;
        private readonly int _length;

        /// <summary>Get or set translation matrix of specified bone</summary>
        /// <param name="index">index to get translation matrix</param>
        /// <returns>translation matrix of specified index</returns>
        public ref Matrix4 this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if((uint)index >= (uint)_length) {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }
                return ref Unsafe.Add(ref Unsafe.AsRef<Matrix4>((void*)_ptr), index);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal SkeletonHandler(Skeleton skeleton, IntPtr translationsPtr, int length)
        {
            Debug.Assert(length >= 0);
            _skeleton = skeleton;
            _ptr = translationsPtr;
            _length = length;
        }

        /// <summary>Get translation matrices as <see cref="Span{T}"/></summary>
        /// <returns>translation matrices</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<Matrix4> AsSpan()
        {
            return MemoryMarshal.CreateSpan(ref Unsafe.AsRef<Matrix4>((void*)_ptr), _length);
        }

        /// <summary>Flush to update translation matrices.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            _skeleton?.UpdateTranslations();
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
                using var sb = ZString.CreateStringBuilder();
                sb.Append("ID: ");
                sb.Append(ID);
                sb.Append(", Parent: ");
                if(Parent == null) {
                    sb.Append("null");
                }
                else {
                    sb.Append(Parent->ID);
                }
                sb.Append(", Next: ");
                if(Next == null) {
                    sb.Append("null");
                }
                else {
                    sb.Append(Next->ID);
                }
                return sb.ToString();
            }
        }

        public override string ToString() => DebugView;
    }

    [DebuggerDisplay("{DebugView}")]
    public readonly struct Bone : IEquatable<Bone>
    {
        /// <summary>Get bone position</summary>
        public readonly Vector3 Position;

        /// <summary>Get parent bone. (null if root bone)</summary>
        public readonly int? ParentBone;

        /// <summary>Get connected bone. (null if not connected, that means this bone is tail.)</summary>
        /// <remarks>This field has no special meaning. Only for debug viewing. If the bone has more than one child, choose one of them.</remarks>
        public readonly int? ConnectedBone;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebugView
        {
            get
            {
                using var sb = ZString.CreateStringBuilder(false);
                sb.Append("Position: ");
                sb.Append(Position);
                sb.Append(", Parent: ");
                if(ParentBone == null) {
                    sb.Append("null");
                }
                else {
                    sb.Append(ParentBone.Value);
                }
                sb.Append(", Connected: ");
                if(ConnectedBone == null) {
                    sb.Append("null");
                }
                else {
                    sb.Append(ConnectedBone.Value);
                }
                return sb.ToString();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Bone(in Vector3 pos, int? parentBone, int? connectedBone)
        {
            Position = pos;
            ParentBone = parentBone;
            ConnectedBone = connectedBone;
        }

        public override bool Equals(object? obj) => obj is Bone bone && Equals(bone);

        public bool Equals(Bone other)
        {
            return Position.Equals(other.Position) &&
                   ParentBone == other.ParentBone &&
                   ConnectedBone == other.ConnectedBone;
        }

        public override int GetHashCode() => HashCode.Combine(Position, ParentBone, ConnectedBone);

        public override string ToString() => DebugView;
    }
}
