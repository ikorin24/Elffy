#nullable enable
using FbxTools;

namespace Elffy.Serialization.Fbx
{
    internal struct MeshGeometry
    {
        public long ID;
        public RawString Name;
        public RawArray<int> IndicesRaw;
        public RawArray<double> Positions;
        public RawArray<double> Normals;
        public RawArray<int> NormalIndices;
        public MappingInformationType NormalMappingType;
        public ReferenceInformationType NormalReferenceType;
        public RawArray<double> UV;
        public RawArray<int> UVIndices;
        public MappingInformationType UVMappingType;
        public ReferenceInformationType UVReferenceType;
        public RawArray<int> Materials;
    }
}
