#nullable enable

namespace Elffy
{
    internal unsafe struct MeshContent
    {
        public uint VertexSize;
        public uint IndexSize;
        public uint VertexFieldCount;
        public VertexFieldInfo* VertexFields;
        public void* Vertices;
        public ulong VerticesByteLength;
        public void* Indices;
        public ulong IndicesByteLength;
    }
}
