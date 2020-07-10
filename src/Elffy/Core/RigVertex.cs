#nullable enable
using Elffy.Diagnostics;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Elffy.Core
{
    [DebuggerDisplay("{Position}")]
    [VertexLike]
    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct RigVertex : IEquatable<RigVertex>
    {
        [FieldOffset(PositionOffset)]
        public Vector3 Position;
        [FieldOffset(NormalOffset)]
        public Vector3 Normal;
        [FieldOffset(TexCoordOffset)]
        public Vector2 TexCoord;

        [FieldOffset(B0Offset)]
        public int Bone0;
        [FieldOffset(B1Offset)]
        public int Bone1;
        [FieldOffset(B2Offset)]
        public int Bone2;
        [FieldOffset(B3Offset)]
        public int Bone3;

        private const int PositionOffset = 0;
        private const int NormalOffset = 12;
        private const int TexCoordOffset = 24;
        private const int B0Offset = 32;
        private const int B1Offset = 36;
        private const int B2Offset = 40;
        private const int B3Offset = 44;

        static RigVertex()
        {
            VertexMarshalHelper<RigVertex>.Register(fieldName => fieldName switch
            {
                nameof(Position) => (PositionOffset, VertexFieldElementType.Float, 3),
                nameof(Normal) => (NormalOffset, VertexFieldElementType.Float, 3),
                nameof(TexCoord) => (TexCoordOffset, VertexFieldElementType.Float, 2),
                nameof(Bone0) => (B0Offset, VertexFieldElementType.Int32, 1),
                nameof(Bone1) => (B1Offset, VertexFieldElementType.Int32, 1),
                nameof(Bone2) => (B2Offset, VertexFieldElementType.Int32, 1),
                nameof(Bone3) => (B3Offset, VertexFieldElementType.Int32, 1),
                _ => throw new ArgumentException(),
            });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RigVertex(Vector3 position, Vector3 normal, Vector2 texcoord, int bone0, int bone1, int bone2, int bone3)
        {
            Position = position;
            Normal = normal;
            TexCoord = texcoord;
            Bone0 = bone0;
            Bone1 = bone1;
            Bone2 = bone2;
            Bone3 = bone3;
        }

        public readonly override bool Equals(object? obj) => obj is RigVertex vertex && Equals(vertex);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Equals(RigVertex other) => Position.Equals(other.Position) &&
                                                        Normal.Equals(other.Normal) &&
                                                        TexCoord.Equals(other.TexCoord) &&
                                                        Bone0.Equals(other.Bone0) &&
                                                        Bone1.Equals(other.Bone1) &&
                                                        Bone2.Equals(other.Bone2) &&
                                                        Bone3.Equals(other.Bone3);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly override int GetHashCode() => HashCode.Combine(Position, Normal, TexCoord, Bone0, Bone1, Bone2, Bone3);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(RigVertex left, RigVertex right) => left.Equals(right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(RigVertex left, RigVertex right) => !(left == right);
    }
}
