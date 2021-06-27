#nullable enable
using FbxTools;
using Elffy.Effective;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Elffy.Serialization.Fbx
{
    internal readonly struct LimbNodeList : IDisposable
    {
        private readonly BufferPooledDictionary<long, LimbNode>? _dic;

        public LimbNodeList(FbxNode objectsNode)
        {
            var dic = new BufferPooledDictionary<long, LimbNode>();
            try {
                using var indexBuf = new ValueTypeRentMemory<int>(objectsNode.Children.Count);
                var modelCount = objectsNode.FindIndexAll(FbxConstStrings.Model(), indexBuf.Span);
                foreach(var i in indexBuf.Span.Slice(0, modelCount)) {
                    var modelNode = objectsNode.Children[i];
                    var modelType = modelNode.Properties[2].AsString().ToModelType();
                    if(modelType != ModelType.LimbNode) { continue; }
                    var limb = new LimbNode(modelNode);
                    dic.Add(limb.ID, limb);
                }
                _dic = dic;
            }
            catch {
                dic.Dispose();
                throw;
            }
        }

        public bool TryGetLimb(long id, out LimbNode limb)
        {
            if(_dic is null) {
                limb = default;
                return false;
            }
            return _dic.TryGetValue(id, out limb);
        }

        public void Dispose()
        {
            _dic?.Dispose();
            Unsafe.AsRef(_dic) = null;
        }
    }

    internal readonly struct LimbNode
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
    }
}
