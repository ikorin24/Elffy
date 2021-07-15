#nullable enable
using System;
using System.Diagnostics;
using FbxTools;
using Elffy.Effective;
using Elffy.Serialization.Fbx.Internal;

namespace Elffy.Serialization.Fbx.Semantic
{
    // - "Geometry"     --------+--- "Vertices"
    //   ([0]:long ID,          |    ([0]:double[] Positions)
    //    [1]:string Name,      |
    //    [2]:string = "Mesh")  |--- "PolygonVertexIndex"
    //                          |    ([0]:int[] IndicesRaw)
    //                          |
    //                          |--- "LayerElementNormal" ---+--- "Normals"
    //                          |                            |    ([0]:double[] Normals)
    //                          |                            |
    //                          |                            |--- "NormalIndex"
    //                          |                            |    ([0]:int[] NormalIndices; if ReferenceInformationType == "IndexToDirect")
    //                          |                            |
    //                          |                            |
    //                          |                            |--- "MappingInformationType"
    //                          |                            |    ([0]:string "ByVertice" or "ByPolygonVertex")
    //                          |                            |
    //                          |                            |--- "ReferenceInformationType"
    //                          |                                 ([0]:string "Direct" or "IndexToDirect")
    //                          |
    //                          |--- "LayerElementUV" -------+--- "UV"
    //                          |                            |    ([0]:double[] UV)
    //                          |                            |
    //                          |                            |--- "UVIndex"
    //                          |                            |    ([0]:int[] UVIndices; i ReferenceInformationType == "IndexToDirect")
    //                          |                            |
    //                          |                            |--- "MappingInformationType"
    //                          |                            |    ([0]:string "ByControllPoint" or "ByPolygonVertex")
    //                          |                            |
    //                          |                            |--- "ReferenceInformationType"
    //                          |                                 ([0]:string "Direct" or "IndexToDirect")
    //                          |
    //                          |--- "LayerElementMaterial" ----- "Materials"
    //                                                            ([0]:int[] Materials)

    internal readonly ref struct MeshGeometry
    {
        public readonly long ID;
        public readonly RawString Name;
        private readonly RawArray<int> _indicesRaw;
        private readonly RawArray<double> _positions;
        private readonly RawArray<double> _normals;
        private readonly RawArray<int> _normalIndices;
        public readonly MappingInformationType NormalMappingType;
        public readonly ReferenceInformationType NormalReferenceType;
        private readonly RawArray<double> _uv;
        private readonly RawArray<int> _uvIndices;
        public readonly MappingInformationType UVMappingType;
        public readonly ReferenceInformationType UVReferenceType;
        public readonly RawArray<int> Materials;

        public ReadOnlySpan<int> IndicesRaw => _indicesRaw.AsSpan();
        public ReadOnlySpan<VecD3> Positions => _positions.AsSpan().MarshalCast<double, VecD3>();
        public ReadOnlySpan<VecD3> Normals => _normals.AsSpan().MarshalCast<double, VecD3>();
        public ReadOnlySpan<int> NormalIndices => _normalIndices.AsSpan();
        public ReadOnlySpan<VecD2> UV => _uv.AsSpan().MarshalCast<double, VecD2>();
        public ReadOnlySpan<int> UVIndices => _uvIndices.AsSpan();

        public MeshGeometry(FbxNode geometryNode)
        {
            Debug.Assert(geometryNode.IsMeshGeometry());

            ID = geometryNode.Properties[0].AsInt64();
            Name = geometryNode.Properties[1].AsString();

            _positions = default;
            _indicesRaw = default;
            _normals = default;
            NormalReferenceType = default;
            NormalMappingType = default;
            _normalIndices = default;
            _uv = default;
            UVReferenceType = default;
            UVMappingType = default;
            _uvIndices = default;
            Materials = default;
            foreach(var node in geometryNode.Children) {
                var nodeName = node.Name;
                if(nodeName.SequenceEqual(FbxConstStrings.Vertices())) {
                    // "Vertices"

                    _positions = node.Properties[0].AsDoubleArray();
                }
                else if(nodeName.SequenceEqual(FbxConstStrings.PolygonVertexIndex())) {
                    // "PolygonVertexindex"

                    _indicesRaw = node.Properties[0].AsInt32Array();
                }
                else if(nodeName.SequenceEqual(FbxConstStrings.LayerElementNormal())) {
                    // "LayerElementNormal"

                    _normals = node.Find(FbxConstStrings.Normals()).Properties[0].AsDoubleArray();

                    var referenceType = node.Find(FbxConstStrings.ReferenceInformationType()).Properties[0].AsString();
                    var mappingType = node.Find(FbxConstStrings.MappingInformationType()).Properties[0].AsString();

                    NormalReferenceType = referenceType.ToReferenceInformationType();
                    NormalMappingType = mappingType.ToMappingInformationType();

                    if(NormalReferenceType == ReferenceInformationType.IndexToDirect) {
                        _normalIndices = node.Find(FbxConstStrings.NormalIndex()).Properties[0].AsInt32Array();
                    }
                }
                else if(nodeName.SequenceEqual(FbxConstStrings.LayerElementUV())) {
                    // "LayerElementUV"

                    _uv = node.Find(FbxConstStrings.UV()).Properties[0].AsDoubleArray();
                    var referenceType = node.Find(FbxConstStrings.ReferenceInformationType()).Properties[0].AsString();
                    var mappingType = node.Find(FbxConstStrings.MappingInformationType()).Properties[0].AsString();
                    UVReferenceType = referenceType.ToReferenceInformationType();
                    UVMappingType = mappingType.ToMappingInformationType();

                    if(UVReferenceType == ReferenceInformationType.IndexToDirect) {
                        _uvIndices = node.Find(FbxConstStrings.UVIndex()).Properties[0].AsInt32Array();
                    }
                }
                else if(nodeName.SequenceEqual(FbxConstStrings.LayerElementMaterial())) {
                    // "LayerElementMaterial"

                    Materials = node.Find(FbxConstStrings.Materials()).Properties[0].AsInt32Array();
                }
            }
        }
    }
}
