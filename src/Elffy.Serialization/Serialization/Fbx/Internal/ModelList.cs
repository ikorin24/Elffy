#nullable enable
using FbxTools;
using System;
using Elffy.Effective;
using Elffy.Serialization.Fbx.Semantic;

namespace Elffy.Serialization.Fbx.Internal
{
    internal readonly ref struct ModelList
    {
        private readonly BufferPooledDictionary<long, MeshModel>? _meshDic;
        private readonly BufferPooledDictionary<long, LimbNode>? _limbDic;
        private readonly BufferPooledDictionary<long, NullModel>? _nullDic;

        public ModelList(FbxNode objectsNode)
        {
            var meshDic = new BufferPooledDictionary<long, MeshModel>();
            var limbDic = new BufferPooledDictionary<long, LimbNode>();
            var nullDic = new BufferPooledDictionary<long, NullModel>();
            try {
                using var indexBuf = new ValueTypeRentMemory<int>(objectsNode.Children.Count, false);
                var modelCount = objectsNode.FindChildIndexAll(FbxConstStrings.Model, indexBuf.AsSpan());
                foreach(var i in indexBuf.AsSpan(0, modelCount)) {
                    var modelNode = objectsNode.Children[i];
                    var modelType = modelNode.Properties[2].AsString().ToModelType();

                    switch(modelType) {
                        case ModelType.LimbNode: {
                            var limb = new LimbNode(modelNode);
                            limbDic.Add(limb.ID, limb);
                            break;
                        }
                        case ModelType.Mesh: {
                            var meshModel = new MeshModel(modelNode);
                            meshDic.Add(meshModel.ID, meshModel);
                            break;
                        }
                        case ModelType.Null: {
                            var nullModel = new NullModel(modelNode);
                            nullDic.Add(nullModel.ID, nullModel);
                            break;
                        }
                        case ModelType.Unknown:
                        default:
                            break;
                    }
                }
                _meshDic = meshDic;
                _limbDic = limbDic;
                _nullDic = nullDic;
            }
            catch {
                meshDic.Dispose();
                limbDic.Dispose();
                nullDic.Dispose();
                throw;
            }
        }

        public bool TryGetMeshModel(long id, out MeshModel meshModel)
        {
            if(_meshDic is null) {
                meshModel = default;
                return false;
            }
            return _meshDic.TryGetValue(id, out meshModel);
        }

        public bool TryGetLimb(long id, out LimbNode limb)
        {
            if(_limbDic is null) {
                limb = default;
                return false;
            }
            return _limbDic.TryGetValue(id, out limb);
        }

        public BufferPooledDictionary<long, NullModel>.ValueCollection GetNullModels()
        {
            var nullDic = _nullDic;
            if(nullDic is null) { throw new InvalidOperationException(); }
            return nullDic.Values;
        }

        public bool TryGetNullModel(long id, out NullModel nullModel)
        {
            if(_nullDic is null) {
                nullModel = default;
                return false;
            }
            return _nullDic.TryGetValue(id, out nullModel);
        }

        public void Dispose()
        {
            _meshDic?.Dispose();
            _limbDic?.Dispose();
            _nullDic?.Dispose();
        }
    }
}
