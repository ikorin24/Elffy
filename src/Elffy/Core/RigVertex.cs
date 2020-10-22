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
        [FieldOffset(BoneOffset)]
        public Vector4i Bone;
        [FieldOffset(WeightOffset)]
        public Vector4 Weight;


        private const int PositionOffset = 0;
        private const int NormalOffset = 12;
        private const int TexCoordOffset = 24;
        private const int BoneOffset = 32;
        private const int WeightOffset = 48;

        static RigVertex()
        {
            VertexMarshalHelper<RigVertex>.Register(fieldName => fieldName switch
            {
                nameof(Position) => (PositionOffset, VertexFieldElementType.Float, 3),
                nameof(Normal) => (NormalOffset, VertexFieldElementType.Float, 3),
                nameof(TexCoord) => (TexCoordOffset, VertexFieldElementType.Float, 2),
                nameof(Bone) => (BoneOffset, VertexFieldElementType.Int32, 4),
                nameof(Weight) => (WeightOffset, VertexFieldElementType.Float, 4),
                _ => throw new ArgumentException(),
            });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RigVertex(in Vector3 position, in Vector3 normal, in Vector2 texcoord, in Vector4i bone, in Vector4 weight)
        {
            Position = position;
            Normal = normal;
            TexCoord = texcoord;
            Bone = bone;
            Weight = weight;
        }

        public readonly override bool Equals(object? obj) => obj is RigVertex vertex && Equals(vertex);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Equals(RigVertex other) => Position.Equals(other.Position) &&
                                                        Normal.Equals(other.Normal) &&
                                                        TexCoord.Equals(other.TexCoord) &&
                                                        Bone.Equals(other.Bone) &&
                                                        Weight.Equals(other.Weight);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly override int GetHashCode() => HashCode.Combine(Position, Normal, TexCoord, Bone, Weight);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(in RigVertex left, in RigVertex right) => left.Equals(right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(in RigVertex left, in RigVertex right) => !(left == right);
    }
}
