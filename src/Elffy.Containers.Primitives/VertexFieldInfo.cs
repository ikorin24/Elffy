#nullable enable
using System.Runtime.InteropServices;

namespace Elffy
{
    [StructLayout(LayoutKind.Explicit)]
    internal struct VertexFieldInfo
    {
        // sizeof(VertexFieldInfo) == 13

        [FieldOffset(0)]
        public VertexFieldSemantics Kind;
        [FieldOffset(1)]
        public uint Offset;
        [FieldOffset(5)]
        public VertexFieldMarshalType MarshalType;
        [FieldOffset(9)]
        public uint MarshalCount;
    }
}
