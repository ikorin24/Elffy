#nullable enable
using System;
using Elffy.Effective.Unsafes;
using Elffy.Serialization.Fbx.Semantic;

namespace Elffy.Serialization.Fbx.Internal
{
    internal readonly struct SkeletonData : IDisposable
    {
        private readonly UnsafeRawList<BoneData> _bones;

        public ReadOnlySpan<BoneData> Bones => _bones.AsSpan();

        public SkeletonData(in SemanticResolver resolver, in NullModel nullModel)
        {
            if(resolver.TryGetChildLimb(nullModel, out var limb) == false) {
                _bones = default;
                return;
            }
            var bones = UnsafeRawList<BoneData>.New(256);
            try {
                bones.Add(new BoneData(null, limb));
                CreateBoneTree(resolver, bones, 0, limb);
                _bones = bones;
            }
            catch {
                bones.Dispose();
                throw;
            }

            static void CreateBoneTree(in SemanticResolver resolver, UnsafeRawList<BoneData> bones, int parentIndex, in LimbNode parentLimb)
            {
                using var children = resolver.GetChildrenLimbs(parentLimb);
                foreach(var childLimb in children.AsSpan()) {
                    bones.Add(new BoneData(parentIndex, childLimb));
                    CreateBoneTree(resolver, bones, bones.Count - 1, childLimb);
                }
            }
        }

        public void Dispose()
        {
            _bones.Dispose();
        }
    }
}
