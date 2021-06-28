#nullable enable
using Elffy.Effective;
using FbxTools;
using System;
using System.Diagnostics;

namespace Elffy.Serialization.Fbx
{
    internal readonly struct Model
    {
        public readonly long ID;
        public readonly RawString Name;
        public readonly ModelType Type;
        public readonly Vector3 Translation;
        public readonly Vector3 Rotation;
        public readonly Vector3 Scale;

        public Model(long id, RawString name, ModelType type, in Vector3 translation, in Vector3 rotation, in Vector3 scale)
        {
            ID = id;
            Name = name;
            Type = type;
            Translation = translation;
            Rotation = rotation;
            Scale = scale;
        }
    }

    internal readonly ref struct ModelList
    {
        private readonly BufferPooledDictionary<long, MeshModel>? _meshDic;
        private readonly BufferPooledDictionary<long, LimbNode>? _limbDic;

        public ModelList(FbxNode objectsNode)
        {
            var meshDic = new BufferPooledDictionary<long, MeshModel>();
            var limbDic = new BufferPooledDictionary<long, LimbNode>();
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
                        case ModelType.Null:
                        case ModelType.Unknown:
                        default:
                            break;
                    }
                }
                _meshDic = meshDic;
                _limbDic = limbDic;
            }
            catch {
                meshDic.Dispose();
                limbDic.Dispose();
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

        public void Dispose()
        {
            _meshDic?.Dispose();
            _limbDic?.Dispose();
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
}
