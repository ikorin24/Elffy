#nullable enable
using FbxTools;
using Elffy.Effective;
using System.Runtime.CompilerServices;
using Elffy.Effective.Unsafes;

namespace Elffy.Serialization.Fbx.Internal
{
    internal readonly ref struct DeformerList
    {
        private readonly BufferPooledDictionary<long, FbxNode>? _dic;

        public DeformerList(FbxNode objectsNode)
        {
            using var buf = new UnsafeRawArray<int>(objectsNode.Children.Count, false);
            var count = objectsNode.FindChildIndexAll(FbxConstStrings.Deformer(), buf.AsSpan());
            _dic = new BufferPooledDictionary<long, FbxNode>(count);
            foreach(var index in buf.AsSpan(0, count)) {
                var deformer = objectsNode.Children[index];
                var id = deformer.Properties[0].AsInt64();
                _dic.Add(id, deformer);
            }
        }

        public bool TryGetDeformer(long id, out FbxNode deformer)
        {
            if(_dic is null) {
                deformer = default;
                return false;
            }
            return _dic.TryGetValue(id, out deformer);
        }

        public void Dispose()
        {
            _dic?.Dispose();
            Unsafe.AsRef(_dic) = null;
        }
    }
}
