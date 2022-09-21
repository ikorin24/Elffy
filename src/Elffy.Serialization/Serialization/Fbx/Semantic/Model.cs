#nullable enable
using FbxTools;
using System;
using System.Diagnostics;
using Elffy.Serialization.Fbx.Internal;

namespace Elffy.Serialization.Fbx.Semantic
{

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
            var property70 = node.FindChild(FbxConstStrings.Properties70);
            Translation = default;
            Rotation = default;
            Scale = Vector3.One;
            foreach(var p in property70.Children) {
                var props = p.Properties;
                if(props.Length < 1 || props[0].Type != FbxPropertyType.String) { continue; }
                var propTypeName = props[0].AsString();
                if(propTypeName.SequenceEqual(FbxConstStrings.Lcl_Translation)) {
                    Translation = new Vector3(
                        (float)props[4].AsDouble(),
                        (float)props[5].AsDouble(),
                        (float)props[6].AsDouble());
                }
                else if(propTypeName.SequenceEqual(FbxConstStrings.Lcl_Rotation)) {
                    Rotation = new Vector3(
                        (float)props[4].AsDouble(),
                        (float)props[5].AsDouble(),
                        (float)props[6].AsDouble());
                }
                else if(propTypeName.SequenceEqual(FbxConstStrings.Lcl_Scaling)) {
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

            var property70 = node.FindChild(FbxConstStrings.Properties70);
            foreach(var p in property70.Children) {
                var props = p.Properties;
                var propTypeName = props[0].AsString();
                if(propTypeName.SequenceEqual(FbxConstStrings.Lcl_Translation)) {
                    Translation = new Vector3(
                        (float)props[4].AsDouble(),
                        (float)props[5].AsDouble(),
                        (float)props[6].AsDouble());
                }
                else if(propTypeName.SequenceEqual(FbxConstStrings.Lcl_Rotation)) {
                    Rotation = new Vector3(
                        (float)props[4].AsDouble(),
                        (float)props[5].AsDouble(),
                        (float)props[6].AsDouble());
                }
                else if(propTypeName.SequenceEqual(FbxConstStrings.Lcl_Scaling)) {
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
}
