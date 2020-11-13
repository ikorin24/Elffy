#nullable enable
using Cysharp.Text;
using Elffy.Core;
using Elffy.Effective;
using Elffy.Exceptions;
using Elffy.Mathematics;
using Elffy.OpenGL;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnmanageUtility;

namespace Elffy.Components
{
    public sealed class Skeleton : ISingleOwnerComponent, IDisposable
    {
        private SingleOwnerComponentCore<Skeleton> _core = new SingleOwnerComponentCore<Skeleton>(true);    // Mutable object, Don't change into reaadonly
        private FloatDataTextureImpl _boneTranslationData = new FloatDataTextureImpl();     // Mutable object, Don't change into reaadonly
        private FloatDataTextureImpl _bonePositionData = new FloatDataTextureImpl();        // Mutable object, Don't change into reaadonly
        private UnmanagedArray<Matrix4>? _translations;
        private UnmanagedArray<Matrix4>? _matrices;
        private UnmanagedArray<BoneInternal>? _tree;
        private bool _disposed;

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
        public ref readonly TextureObject TranslationData => ref _boneTranslationData.TextureObject;

        /// <summary>Get <see cref="TextureObject"/> of bone position matrices. (This is Texture1D)</summary>
        public ref readonly TextureObject PositionData => ref _bonePositionData.TextureObject;

        public Skeleton()
        {
        }

        ~Skeleton() => Dispose(false);

        /// <summary>Load bone data</summary>
        /// <param name="bones">bone data</param>
        public unsafe void Load(ReadOnlySpan<Bone> bones)
        {
            _tree?.Dispose();
            _matrices?.Dispose();
            _translations?.Dispose();

            CreateBoneTree(bones, out var posMatrix, out _tree);
            using(posMatrix) {
                // Load matrices of positions
                _bonePositionData.Load(posMatrix.AsSpan().MarshalCast<Matrix4, Color4>());
            }

            // Round up pixel count of bone matrix buffer on data texture to 2^n.
            var bufLen = MathTool.RoundUpToPowerOfTwo(bones.Length * sizeof(Matrix4) / sizeof(Vector4));

            // Load matrices as identity
            _matrices = new UnmanagedArray<Matrix4>(bufLen, fill: Matrix4.Identity);
            _boneTranslationData.Load(_matrices.AsSpan().MarshalCast<Matrix4, Color4>());

            // Initialize translations as identity
            _translations = new UnmanagedArray<Matrix4>(bones.Length, fill: Matrix4.Identity);
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
            if(_tree is null) { return; }
            Debug.Assert(_matrices is not null);
            Debug.Assert(_translations is not null);

            var matrices = (Matrix4*)_matrices.Ptr;
            var tree = (BoneInternal*)_tree.Ptr;
            var translations = (Matrix4*)_translations.Ptr;

            for(BoneInternal* b = tree; b != null; b = b->Next) {
                if(b->Parent == null) {
                    matrices[b->ID] = translations[b->ID];
                }
                else {
                    matrices[b->ID] = matrices[b->Parent->ID] * translations[b->ID];
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

        private void Dispose(bool disposing)
        {
            if(_disposed) { return; }

            if(disposing) {
                _boneTranslationData.Dispose();
                _bonePositionData.Dispose();

                _matrices?.Dispose();
                _tree?.Dispose();
                _matrices = null;
                _tree = null;
            }
            else {
                throw new MemoryLeakException(typeof(Skeleton));
            }
            _disposed = true;
        }

        private unsafe static void CreateBoneTree(ReadOnlySpan<Bone> bones,
                                                  out UnsafeRawArray<Matrix4> posMatrices,
                                                  out UnmanagedArray<BoneInternal> boneTree)
        {
            boneTree = null!;
            posMatrices = default;
            try {

                // Round up pixel count of bone position matrix on data texture to 2^n.
                var pixCountPoT = MathTool.RoundUpToPowerOfTwo(bones.Length * (sizeof(Matrix4) / sizeof(Color4)));
                posMatrices = new UnsafeRawArray<Matrix4>(pixCountPoT);

                boneTree = new UnmanagedArray<BoneInternal>(bones.Length);
                var tree = (BoneInternal*)boneTree.Ptr;

                // Initialize tree of 'ID' and 'Parent'.
                // ('Next' is not set yet.)
                for(int i = 0; i < bones.Length; i++) {
                    tree[i].ID = i;                      // tree[i].ID is index in the tree.
                    if(bones[i].ParentBone != null) {
                        tree[i].Parent = &tree[(int)bones[i].ParentBone!];
                    }
                    bones[i].Position.ToTranslationMatrix4(out posMatrices[i]);
                }

                // [NOTE]
                // If debug this and use debugger viewer,
                // enable initializing 'UnsafeRawArray' and 'UnsafeRawList' zero-cleared by constructor arg.
                // Otherwise, you see undefined values, or it may cause exceptions in debugger.
                // (but it is no matter)

                // 'childrenBuf[i]' means IDs of children of tree[i].
                // It is like 'List<int>[]'
                var childrenBuf = new UnsafeRawArray<UnsafeRawList<int>>(boneTree.Length);

                try {
                    // Set children
                    for(int i = 0; i < childrenBuf.Length; i++) {
                        childrenBuf[i] = new UnsafeRawList<int>(4);
                        if(tree[i].Parent != null) {
                            var parentID = tree[i].Parent->ID;
                            childrenBuf[parentID].Add(i);
                        }
                    }

                    // Initialize tree of 'Next'
                    for(int i = 0; i < boneTree.Length; i++) {
                        if(childrenBuf[i].Count > 0) {              // If tree[i] has any children
                            var firstChildID = childrenBuf[i][0];
                            tree[i].Next = &tree[firstChildID];     // tree[i].Next is first child of itself.
                        }
                        else {                                      // When tree[i] has no children
                            ref var target = ref tree[i];           // For the first time, target is tree[i].
                        SEARCH:
                            if(target.Parent == null) {             // If target is root
                                tree[i].Next = null;                // tree[i].Next is null
                                continue;
                            }

                            ref var siblings = ref childrenBuf[target.Parent->ID];  // Get siblings of target.

                            var num = siblings.IndexOf(target.ID);          // Get number of target in the siblings.
                            if(num < siblings.Count - 1) {                  // That means tree[i] is not the last child in its siblings.
                                tree[i].Next = &tree[siblings[num + 1]];    // tree[i].Next is next sibling of itself.
                            }
                            else {                                          // When tree[i] is the last child in its siblings.
                                target = ref *(target.Parent);              // If target is not root, change target to its parent
                                goto SEARCH;                                // and search recursively.
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
                posMatrices.Dispose();
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

    [DebuggerDisplay("{DebugView()}")]
    internal unsafe struct BoneInternal
    {
        public int ID;
        public BoneInternal* Parent;
        public BoneInternal* Next;

        private string DebugView()  // only for debug
        {
            return ZString.Concat("ID: ", ID,
                                  ", Parent: ", Parent is null ? (int?)null : Parent->ID,
                                  ", Next: ", Next is null ? (int?)null : Next->ID);
        }
    }

    [DebuggerDisplay("Position: {Position}, Parent: {ParentBone}, Connected: {ConnectedBone}")]
    public readonly struct Bone
    {
        /// <summary>Get bone position</summary>
        public readonly Vector3 Position;

        /// <summary>Get parent bone. (null if root bone)</summary>
        public readonly int? ParentBone;

        /// <summary>Get connected bone. (null if not connected, that means this bone is tail.)</summary>
        /// <remarks>This field has no special meaning. Only for debug viewing. If the bone has more than one child, choose one of them.</remarks>
        public readonly int? ConnectedBone;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Bone(in Vector3 pos, int? parentBone, int? connectedBone)
        {
            Position = pos;
            ParentBone = parentBone;
            ConnectedBone = connectedBone;
        }
    }
}
