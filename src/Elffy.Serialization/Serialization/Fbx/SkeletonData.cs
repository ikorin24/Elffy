#nullable enable
using System;
using Elffy.Effective.Unsafes;
using Elffy.Serialization.Fbx.Semantic;
using Elffy.Serialization.Fbx.Internal;
using Elffy.Components;
using System.Diagnostics;

namespace Elffy.Serialization.Fbx
{
    public readonly struct SkeletonData : IEquatable<SkeletonData>
    {
        private readonly UnsafeRawList<BoneData> _bones;
        private readonly UnsafeRawArray<Matrix4> _matrices;
        internal Span<Matrix4> BoneMatricesInternal => _matrices.AsSpan();

        public int BoneCount => _bones.Count;

        public ReadOnlySpan<BoneData> Bones => _bones.AsSpan();

        /// <summary>Matrix of each bone in the model-local coordinate. (Not bone-local)</summary>
        public ReadOnlySpan<Matrix4> BoneMatrices => _matrices.AsSpan();

        internal SkeletonData(in SemanticResolver resolver, in NullModel nullModel)
        {
            if(resolver.TryGetChildLimb(nullModel, out var limb) == false) {
                _bones = default;
                _matrices = default;
                return;
            }
            var bones = UnsafeRawList<BoneData>.New();
            try {
                bones.Add(new BoneData(null, limb.ID));
                CreateBoneTree(resolver, bones, 0, limb);
                _bones = bones;
            }
            catch {
                bones.Dispose();
                throw;
            }
            var matrices = new UnsafeRawArray<Matrix4>(bones.Count);
            try {
                // I don't know which way is better.
                matrices.AsSpan().Clear();
                //matrices.AsSpan().Fill(Matrix4.Identity);

                _matrices = matrices;
            }
            catch {
                matrices.Dispose();
                throw;
            }


            static void CreateBoneTree(in SemanticResolver resolver, UnsafeRawList<BoneData> bones, int parentIndex, in LimbNode parentLimb)
            {
                using var children = resolver.GetChildrenLimbs(parentLimb);
                foreach(var childLimb in children.AsSpan()) {
                    bones.Add(new BoneData(parentIndex, childLimb.ID));
                    CreateBoneTree(resolver, bones, bones.Count - 1, childLimb);
                }
            }
        }

        /// <summary>Create bones to load to <see cref="Skeleton"/></summary>
        /// <param name="dest">the destination buffer to create bones</param>
        /// <returns>the number of created bones</returns>
        public int CreateBones(Span<Bone> dest)
        {
            var boneCount = BoneCount;
            if(dest.Length < boneCount) { throw new ArgumentException("The destination is too short."); }

            var bones = _bones.AsSpan();
            var matrices = _matrices.AsSpan();
            Debug.Assert(bones.Length == matrices.Length);
            for(int i = 0; i < boneCount; i++) {
                dest[i] = new Bone(bones[i].ParentIndex, matrices[i]);
            }
            return boneCount;
        }

        /// <summary>Create bones to load to <see cref="Skeleton"/></summary>
        /// <returns>the array of <see cref="Bone"/> to load to <see cref="Skeleton"/></returns>
        public Bone[] CreateBones()
        {
            var bones = new Bone[BoneCount];
            CreateBones(bones);
            return bones;
        }

        internal void DisposeInternal()
        {
            _bones.Dispose();
            _matrices.Dispose();
        }

        public override bool Equals(object? obj) => obj is SkeletonData data && Equals(data);

        public bool Equals(SkeletonData other) => _bones.Equals(other._bones) && _matrices.Equals(other._matrices);

        public override int GetHashCode() => HashCode.Combine(_bones, _matrices);
    }
}
