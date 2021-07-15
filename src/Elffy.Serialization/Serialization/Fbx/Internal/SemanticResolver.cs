#nullable enable
using System;
using FbxTools;
using Elffy.Effective;
using Elffy.Effective.Unsafes;
using Elffy.Serialization.Fbx.Semantic;

namespace Elffy.Serialization.Fbx.Internal
{
    internal readonly ref struct SemanticResolver
    {
        private readonly FbxNode _objectsNode;
        private readonly FbxConnectionResolver _connResolver;
        private readonly DeformerList _deformerList;
        private readonly ModelList _modelList;


        public FbxNode ObjectsNode => _objectsNode;

        public SemanticResolver(FbxObject fbx)
        {
            _objectsNode = fbx.Find(FbxConstStrings.Objects());
            var connectionsNode = fbx.Find(FbxConstStrings.Connections());
            _deformerList = new(_objectsNode);
            _connResolver = new(connectionsNode);
            _modelList = new(_objectsNode);
        }

        public BufferPooledDictionary<long, NullModel>.ValueCollection GetNullModels()
        {
            return _modelList.GetNullModels();
        }

        public bool TryGetChildLimb(in NullModel parent, out LimbNode limb)
        {
            foreach(var childID in GetSources(parent.ID)) {
                if(_modelList.TryGetLimb(childID, out limb)) {
                    return true;
                }
            }
            limb = default;
            return false;
        }

        public UnsafeRawList<LimbNode> GetChildrenLimbs(in LimbNode parent)
        {
            var list = UnsafeRawList<LimbNode>.New(32);
            try {
                foreach(var childID in GetSources(parent.ID)) {
                    if(_modelList.TryGetLimb(childID, out var limb)) {
                        list.Add(limb);
                    }
                }
                return list;
            }
            catch {
                list.Dispose();
                throw;
            }
        }

        public bool TryGetLimb(in ClusterDeformer cluster, out LimbNode limb)
        {
            foreach(var childID in GetSources(cluster.ID)) {
                if(_modelList.TryGetLimb(childID, out limb)) {
                    return true;
                }
            }
            limb = default;
            return false;
        }

        public bool TryGetMeshModel(in MeshGeometry mesh, out MeshModel model)
        {
            foreach(var id in GetDests(mesh.ID)) {
                if(_modelList.TryGetMeshModel(id, out model)) {
                    return true;
                }
            }
            model = default;
            return false;
        }

        public bool TryGetSkinDeformer(in MeshGeometry mesh, out SkinDeformer skin)
        {
            foreach(var id in GetSources(mesh.ID)) {
                if(_deformerList.TryGetDeformer(id, out var deformerNode) == false) { continue; }
                if(deformerNode.Properties.Length > 2 && deformerNode.Properties[2].AsString().SequenceEqual(FbxConstStrings.Skin())) {
                    skin = new SkinDeformer(deformerNode);
                    return true;
                }
            }
            skin = default;
            return false;
        }

        public int GetClusterDeformerCount(in SkinDeformer skin)
        {
            int count = 0;
            foreach(var id in GetSources(skin.ID)) {
                if(_deformerList.TryGetDeformer(id, out var deformerNode) == false) { continue; }
                if(deformerNode.Properties.Length > 2 && deformerNode.Properties[2].AsString().SequenceEqual(FbxConstStrings.Cluster()) && deformerNode.Children.Count >= 6) {
                    count++;
                }
            }
            return count;
        }

        public int GetClusterDeformers(in SkinDeformer skin, Span<ClusterDeformer> clusters)
        {
            int count = 0;
            foreach(var id in GetSources(skin.ID)) {
                if(_deformerList.TryGetDeformer(id, out var deformerNode) == false) { continue; }
                if(deformerNode.Properties.Length > 2 && deformerNode.Properties[2].AsString().SequenceEqual(FbxConstStrings.Cluster()) && deformerNode.Children.Count >= 6) {
                    clusters[count++] = new ClusterDeformer(deformerNode);
                }
            }
            return count;
        }

        public ValueTypeRentMemory<ClusterDeformer> GetClusterDeformers(in SkinDeformer skin, out int count)
        {
            var sourceIDList = GetSources(skin.ID);
            count = 0;
            var clusters = new ValueTypeRentMemory<ClusterDeformer>(sourceIDList.Length);
            try {
                foreach(var id in sourceIDList) {
                    if(_deformerList.TryGetDeformer(id, out var deformerNode) == false) { continue; }
                    if(deformerNode.Properties.Length > 2 && deformerNode.Properties[2].AsString().SequenceEqual(FbxConstStrings.Cluster()) && deformerNode.Children.Count >= 6) {
                        clusters[count++] = new ClusterDeformer(deformerNode);
                    }
                }
                return clusters;
            }
            catch {
                clusters.Dispose();
                count = 0;
                throw;
            }
        }

        public void Dispose()
        {
            _connResolver.Dispose();
            _deformerList.Dispose();
            _modelList.Dispose();
        }

        private ReadOnlySpan<long> GetSources(long destID) => _connResolver.GetSources(destID);
        private ReadOnlySpan<long> GetDests(long sourceID) => _connResolver.GetDests(sourceID);
    }
}
