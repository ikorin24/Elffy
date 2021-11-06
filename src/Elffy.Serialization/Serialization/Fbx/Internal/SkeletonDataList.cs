#nullable enable
using System;
using Elffy.Effective;

namespace Elffy.Serialization.Fbx.Internal
{
    internal readonly struct SkeletonDataList : IDisposable
    {
        private readonly ValueTypeRentMemory<SkeletonData> _skeletons;
        private readonly int _count;

        public ReadOnlySpan<SkeletonData> Span => _skeletons.AsSpan(0, _count);

        internal SkeletonDataList(in SemanticResolver resolver)
        {
            var nullModels = resolver.GetNullModels();
            var skeletons = new ValueTypeRentMemory<SkeletonData>(nullModels.Count, false);
            try {
                var i = 0;
                foreach(var nullModel in nullModels) {
                    var skeleton = new SkeletonData(resolver, nullModel);
                    if(skeleton.BoneCount == 0) {
                        skeleton.DisposeInternal();
                    }
                    else {
                        skeletons[i] = new SkeletonData(resolver, nullModel);
                        i++;
                    }
                }
                _count = i;
                _skeletons = skeletons;
            }
            catch {
                foreach(var skeleton in skeletons.AsSpan()) {
                    skeleton.DisposeInternal();
                }
                skeletons.Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            foreach(var model in Span) {
                model.DisposeInternal();
            }
            _skeletons.Dispose();
        }
    }
}
