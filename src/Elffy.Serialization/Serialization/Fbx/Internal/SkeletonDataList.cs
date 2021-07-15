#nullable enable
using System;
using Elffy.Effective;

namespace Elffy.Serialization.Fbx.Internal
{
    internal readonly struct SkeletonDataList : IDisposable
    {
        private readonly ValueTypeRentMemory<SkeletonData> _skeletons;

        public ReadOnlySpan<SkeletonData> Span => _skeletons.AsSpan();

        public SkeletonDataList(in SemanticResolver resolver)
        {
            var nullModels = resolver.GetNullModels();
            var skeletons = new ValueTypeRentMemory<SkeletonData>(nullModels.Count);
            try {
                var i = 0;
                foreach(var nullModel in nullModels) {
                    skeletons[i] = new SkeletonData(resolver, nullModel);
                    i++;
                }
                _skeletons = skeletons;
            }
            catch {
                skeletons.Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            foreach(var model in _skeletons.Span) {
                model.Dispose();
            }
            _skeletons.Dispose();
        }
    }
}
