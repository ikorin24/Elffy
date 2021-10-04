#nullable enable
using Elffy.Diagnostics;
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace Elffy
{
    [VertexLike]
    [StructLayout(LayoutKind.Explicit)]
    [DebuggerDisplay("{Position}")]
    public struct SkinnedVertex : IEquatable<SkinnedVertex>
    {
        [FieldOffset(0)]
        public Vector3 Position;
        [FieldOffset(12)]
        public Vector3 Normal;
        [FieldOffset(24)]
        public Vector2 UV;
        [FieldOffset(32)]
        public Vector4i Bone;
        [FieldOffset(48)]
        public Vector4 Weight;
        [FieldOffset(64)]
        public int TextureIndex;

        static SkinnedVertex()
        {
            VertexMarshalHelper.Register<SkinnedVertex>(
                fieldName => fieldName switch
                {
                    nameof(Position) => (0, VertexFieldMarshalType.Float, 3),
                    nameof(Normal) => (12, VertexFieldMarshalType.Float, 3),
                    nameof(UV) => (24, VertexFieldMarshalType.Float, 2),
                    nameof(Bone) => (32, VertexFieldMarshalType.Int32, 4),
                    nameof(Weight) => (48, VertexFieldMarshalType.Float, 4),
                    nameof(TextureIndex) => (64, VertexFieldMarshalType.Int32, 1),
                    _ => throw new ArgumentException(),
                },
                specialField => specialField switch
                {
                    VertexSpecialField.Position => nameof(Position),
                    VertexSpecialField.UV => nameof(UV),
                    VertexSpecialField.Normal => nameof(Normal),
                    VertexSpecialField.TextureIndex => nameof(TextureIndex),
                    VertexSpecialField.Bone => nameof(Bone),
                    VertexSpecialField.Weight => nameof(Weight),
                    _ => "",
                });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SkinnedVertex(in Vector3 position, in Vector3 normal, in Vector2 uv, in Vector4i bone, in Vector4 weight, int textureIndex)
        {
            Position = position;
            Normal = normal;
            UV = uv;
            Bone = bone;
            Weight = weight;
            TextureIndex = textureIndex;
        }

        public readonly override bool Equals(object? obj) => obj is SkinnedVertex vertex && Equals(vertex);

        public readonly bool Equals(SkinnedVertex other) => Position.Equals(other.Position) && Normal.Equals(other.Normal) && UV.Equals(other.UV) && Bone.Equals(other.Bone) && Weight.Equals(other.Weight) && TextureIndex.Equals(other.TextureIndex);

        public readonly override int GetHashCode() => HashCode.Combine(Position, Normal, UV, Bone, Weight, TextureIndex);

        public static bool operator ==(in SkinnedVertex left, in SkinnedVertex right) => left.Equals(right);

        public static bool operator !=(in SkinnedVertex left, in SkinnedVertex right) => !(left == right);
    }
}
