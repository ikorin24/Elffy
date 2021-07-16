#nullable enable
using FbxTools;
using System.Diagnostics;
using Elffy.Serialization.Fbx.Internal;
using System;

namespace Elffy.Serialization.Fbx.Semantic
{

    internal readonly struct SkinDeformer
    {
        public readonly long ID;
        public readonly RawString Name;

        public SkinDeformer(FbxNode skinDeformerNode)
        {
#if DEBUG
            Debug.Assert(skinDeformerNode.Properties[2].AsString() == "Skin");
#endif
            ID = skinDeformerNode.Properties[0].AsInt64();
            Name = skinDeformerNode.Properties[1].AsString();
        }
    }

    internal readonly struct ClusterDeformer
    {
        private readonly FbxNode _node;
        public readonly long ID;
        public readonly RawString Name;

        public ClusterDeformer(FbxNode clusterDeformerNode)
        {
#if DEBUG
            Debug.Assert(clusterDeformerNode.Properties[2].AsString() == "Cluster");
#endif
            _node = clusterDeformerNode;
            ID = clusterDeformerNode.Properties[0].AsInt64();
            Name = clusterDeformerNode.Properties[1].AsString();
        }

        public RawArray<int> GetIndices()
        {
            return _node.Find(FbxConstStrings.Indexes()).Properties[0].AsInt32Array();
        }

        public RawArray<double> GetWeights()
        {
            return _node.Find(FbxConstStrings.Weights()).Properties[0].AsDoubleArray();
        }

        public void GetInitialMatrix(out Matrix4 mat)
        {
            var array = _node.Find(FbxConstStrings.TransformLink()).Properties[0].AsDoubleArray();
            mat.M00 = (float)array[0];
            mat.M10 = (float)array[1];
            mat.M20 = (float)array[2];
            mat.M30 = (float)array[3];
            mat.M01 = (float)array[4];
            mat.M11 = (float)array[5];
            mat.M21 = (float)array[6];
            mat.M31 = (float)array[7];
            mat.M02 = (float)array[8];
            mat.M12 = (float)array[9];
            mat.M22 = (float)array[10];
            mat.M32 = (float)array[11];
            mat.M03 = (float)array[12];
            mat.M13 = (float)array[13];
            mat.M23 = (float)array[14];
            mat.M33 = (float)array[15];
        }
    }
}
