#nullable enable
using Elffy.Effective;
using Elffy.Effective.Unsafes;
using FbxTools;
using System;
using System.Diagnostics;
using System.Linq;

namespace Elffy.Serialization.Fbx
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
                using var indexBuf = new ValueTypeRentMemory<int>(objectsNode.Children.Count);
                var modelCount = objectsNode.FindIndexAll(FbxConstStrings.Model(), indexBuf.Span);
                foreach(var i in indexBuf.Span.Slice(0, modelCount)) {
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

    [DebuggerDisplay("{ToString(),nq}")]
    internal readonly struct LimbNode : IEquatable<LimbNode>
    {
        public readonly FbxNode Node;
        public readonly long ID;
        public readonly RawString Name;
        public readonly Vector3 Translation;
        public readonly Vector3 Rotation;
        public readonly Vector3 Scale;

        public LimbNode(FbxNode node)
        {
            Debug.Assert(node.Properties[2].AsString().ToModelType() == ModelType.LimbNode);
            Node = node;
            ID = node.Properties[0].AsInt64();
            Name = node.Properties[1].AsString();
            var property70 = node.Find(FbxConstStrings.Properties70());
            Translation = default;
            Rotation = default;
            Scale = Vector3.One;
            foreach(var p in property70.Children) {
                var props = p.Properties;
                if(props.Length < 1 || props[0].Type != FbxPropertyType.String) { continue; }
                var propTypeName = props[0].AsString();
                if(propTypeName.SequenceEqual(FbxConstStrings.Lcl_Translation())) {
                    Translation = new Vector3(
                        (float)props[4].AsDouble(),
                        (float)props[5].AsDouble(),
                        (float)props[6].AsDouble());
                }
                else if(propTypeName.SequenceEqual(FbxConstStrings.Lcl_Rotation())) {
                    Rotation = new Vector3(
                        (float)props[4].AsDouble(),
                        (float)props[5].AsDouble(),
                        (float)props[6].AsDouble());
                }
                else if(propTypeName.SequenceEqual(FbxConstStrings.Lcl_Scaling())) {
                    Scale = new Vector3(
                        (float)props[4].AsDouble(),
                        (float)props[5].AsDouble(),
                        (float)props[6].AsDouble());
                }
            }
        }

        public override bool Equals(object? obj) => obj is LimbNode node && Equals(node);

        public bool Equals(LimbNode other) => Node.Equals(other.Node);

        public override int GetHashCode() => Node.GetHashCode();

        public override string ToString() => $"{nameof(LimbNode)} (ID: {ID}, Name: {Name})";
    }

    [DebuggerDisplay("{ToString(),nq}")]
    internal readonly struct MeshModel : IEquatable<MeshModel>
    {
        public readonly FbxNode Node;
        public readonly long ID;
        public readonly RawString Name;
        public readonly Vector3 Translation;
        public readonly Vector3 Rotation;
        public readonly Vector3 Scale;

        public MeshModel(FbxNode node)
        {
            Debug.Assert(node.Properties[2].AsString().ToModelType() == ModelType.Mesh);
            Node = node;
            ID = node.Properties[0].AsInt64();
            Name = node.Properties[1].AsString();
            Translation = default;
            Rotation = default;
            Scale = Vector3.One;

            var property70 = node.Find(FbxConstStrings.Properties70());
            foreach(var p in property70.Children) {
                var props = p.Properties;
                var propTypeName = props[0].AsString();
                if(propTypeName.SequenceEqual(FbxConstStrings.Lcl_Translation())) {
                    Translation = new Vector3(
                        (float)props[4].AsDouble(),
                        (float)props[5].AsDouble(),
                        (float)props[6].AsDouble());
                }
                else if(propTypeName.SequenceEqual(FbxConstStrings.Lcl_Rotation())) {
                    Rotation = new Vector3(
                        (float)props[4].AsDouble(),
                        (float)props[5].AsDouble(),
                        (float)props[6].AsDouble());
                }
                else if(propTypeName.SequenceEqual(FbxConstStrings.Lcl_Scaling())) {
                    Scale = new Vector3(
                        (float)props[4].AsDouble(),
                        (float)props[5].AsDouble(),
                        (float)props[6].AsDouble());
                }
            }
        }

        public override bool Equals(object? obj) => obj is MeshModel model && Equals(model);

        public bool Equals(MeshModel other) => Node.Equals(other.Node);

        public override int GetHashCode() => Node.GetHashCode();

        public override string ToString() => $"{nameof(MeshModel)} (ID: {ID}, Name: {Name})";
    }

    [DebuggerDisplay("{ToString(),nq}")]
    internal readonly struct NullModel : IEquatable<NullModel>
    {
        public readonly FbxNode Node;
        public readonly long ID;
        public readonly RawString Name;

        public NullModel(FbxNode node)
        {
            Debug.Assert(node.Properties[2].AsString().ToModelType() == ModelType.Null);
            Node = node;
            ID = node.Properties[0].AsInt64();
            Name = node.Properties[1].AsString();
        }

        public override bool Equals(object? obj) => obj is NullModel model && Equals(model);

        public bool Equals(NullModel other) => Node.Equals(other.Node) && ID == other.ID && Name.Equals(other.Name);

        public override int GetHashCode() => HashCode.Combine(Node, ID, Name);

        public override string ToString() => $"{nameof(NullModel)} (ID: {ID}, Name: {Name})";
    }

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
