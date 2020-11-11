#nullable enable
using Elffy.Core;
using Elffy.Effective;
using Elffy.Exceptions;
using Elffy.OpenGL;
using System;
using System.Diagnostics;
using UnmanageUtility;

namespace Elffy.Components
{
    public sealed class Skeleton : ISingleOwnerComponent, IDisposable
    {
        private SingleOwnerComponentCore<Skeleton> _core = new SingleOwnerComponentCore<Skeleton>(true);    // Mutable object, Don't change into reaadonly
        private FloatDataTextureImpl _boneMoveData = new FloatDataTextureImpl();    // Mutable object, Don't change into reaadonly
        private UnmanagedArray<Vector3>? _positions;
        private UnmanagedArray<Matrix4>? _matrices;
        private UnmanagedArray<BoneInternal>? _tree;
        private bool _disposed;

        public ComponentOwner? Owner => _core.Owner;

        public bool AutoDisposeOnDetached => _core.AutoDisposeOnDetached;

        public int BoneCount => _positions?.Length ?? 0;

        public Skeleton()
        {
        }

        ~Skeleton() => Dispose(false);

        public void Load(ReadOnlySpan<Bone> bones)
        {
            CreateBones(bones, out _positions, out _tree);
            _matrices = new UnmanagedArray<Matrix4>(bones.Length, fill: Matrix4.Identity);
            _boneMoveData.Load(_matrices.AsSpan().MarshalCast<Matrix4, Color4>());
        }

        public void Apply(TextureUnitNumber textureUnit) => _boneMoveData.Apply(textureUnit);

        void IComponent.OnAttached(ComponentOwner owner)
        {
            _core.OnAttached(owner);
            owner.LateUpdated += OwnerLateUpdated;
        }

        void IComponent.OnDetached(ComponentOwner owner)
        {
            owner.LateUpdated -= OwnerLateUpdated;
            _core.OnDetachedForDisposable(owner, this);
        }

        private unsafe void OwnerLateUpdated(FrameObject sender)
        {
            if(_tree is null) { return; }
            Debug.Assert(_positions is not null);
            Debug.Assert(_matrices is not null);

            //var pos = (Vector3*)_positions.Ptr;
            var matrices = (Matrix4*)_matrices.Ptr;
            var tree = (BoneInternal*)_tree.Ptr;

            for(BoneInternal* b = tree; b != null; b = b->Next) {
                //pos[b->ID].ToTranslationMatrix4(out matrices[b->ID]);
                if(b->Parent != null) {
                    matrices[b->ID] = matrices[b->Parent->ID] * matrices[b->ID];
                }
            }

            //_boneMoveData.Update(_matrices.AsSpan().MarshalCast<Matrix4, Color4>(), 0);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if(_disposed) { return; }

            if(disposing) {
                _boneMoveData.Dispose();

                _positions?.Dispose();
                _matrices?.Dispose();
                _tree?.Dispose();
                _positions = null;
                _matrices = null;
                _tree = null;
            }
            else {
                throw new MemoryLeakException(typeof(Skeleton));
            }
            _disposed = true;
        }

        private unsafe static void CreateBones(ReadOnlySpan<Bone> bones,
                                               out UnmanagedArray<Vector3> positions,
                                               out UnmanagedArray<BoneInternal> tree)
        {
            positions = null!;
            tree = null!;
            try {
                positions = new UnmanagedArray<Vector3>(bones.Length);
                tree = new UnmanagedArray<BoneInternal>(bones.Length);

                var treePtr = (BoneInternal*)tree.Ptr;
                var posPtr = (Vector3*)positions.Ptr;

                // Initialize tree of 'ID' and 'Parent'.
                // ('Next' is not set yet.)
                for(int i = 0; i < bones.Length; i++) {
                    treePtr[i].ID = i;                      // tree[i].ID is index in the tree.
                    if(bones[i].ParentBone != null) {
                        treePtr[i].Parent = &treePtr[(int)bones[i].ParentBone!];
                    }
                    posPtr[i] = bones[i].Position;
                }


                // 'childrenBuf[i]' means IDs of children of tree[i].
                var childrenBuf = new UnsafeRawArray<UnsafeRawList<int>>(tree.Length, true);

                try {
                    // Set children
                    for(int i = 0; i < childrenBuf.Length; i++) {
                        childrenBuf[i] = new UnsafeRawList<int>(4);
                        if(treePtr[i].Parent != null) {
                            var parentID = treePtr[i].Parent->ID;
                            childrenBuf[parentID].Add(i);
                        }
                    }

                    // Initialize tree of 'Next'
                    for(int i = 0; i < tree.Length; i++) {
                        if(childrenBuf[i].Count > 0) {                  // If tree[i] has any children
                            var firstChildID = childrenBuf[i][0];
                            treePtr[i].Next = &treePtr[firstChildID];   // tree[i].Next is first child of itself.
                        }
                        else {                                          // When tree[i] has no children
                            ref var target = ref treePtr[i];            // For the first time, target is tree[i].
                        SEARCH:
                            if(target.Parent == null) {                 // If target is root
                                treePtr[i].Next = null;                 // tree[i].Next is null
                                continue;
                            }

                            ref var siblings = ref childrenBuf[target.Parent->ID];  // Get siblings of target.

                            var num = siblings.IndexOf(target.ID);              // Get number of target in the siblings.
                            if(num < siblings.Count - 1) {                      // That means tree[i] is not the last child in its siblings.
                                treePtr[i].Next = &treePtr[siblings[num + 1]];  // tree[i].Next is next sibling of itself.
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
                positions?.Dispose();
                tree?.Dispose();
                throw;
            }

            int a = 0;
            for(BoneInternal* b = (BoneInternal*)tree.Ptr; b != null; b = b->Next) {
                a++;
            }
            Debug.WriteLine(a);
        }
    }

    internal unsafe struct BoneInternal
    {
        public int ID;
        public BoneInternal* Parent;
        public BoneInternal* Next;
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

        public Bone(in Vector3 pos, int? parentBone, int? connectedBone)
        {
            Position = pos;
            ParentBone = parentBone;
            ConnectedBone = connectedBone;
        }
    }
}
