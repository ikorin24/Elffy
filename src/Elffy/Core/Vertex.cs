#nullable enable
using Elffy.Diagnostics;
using System;
using System.Diagnostics;

namespace Elffy.Core
{
    [DebuggerDisplay("{Position}")]
    [VertexLike]
    public unsafe struct Vertex : IEquatable<Vertex>
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Color4 Color;
        public Vector2 TexCoord;

        private static readonly int PositionOffset = 0;
        private static readonly int NormalOffset = sizeof(Vector3);
        private static readonly int ColorOffset = sizeof(Vector3) + sizeof(Vector3);
        private static readonly int TexCoordOffset = sizeof(Vector3) + sizeof(Vector3) + sizeof(Color4);

        static Vertex()
        {
            VertexMarshalHelper<Vertex>.Register(fieldName => fieldName switch
            {
                nameof(Position) => (PositionOffset, VertexFieldElementType.Float, 3),
                nameof(Normal) =>   (NormalOffset,   VertexFieldElementType.Float, 3),
                nameof(Color) =>    (ColorOffset,    VertexFieldElementType.Float, 4),
                nameof(TexCoord) => (TexCoordOffset, VertexFieldElementType.Float, 2),
                _ => throw new ArgumentException(),
            });
        }

        public Vertex(in Vector3 position, in Vector3 normal, in Vector2 texcoord)
        {
            Position = position;
            Normal = normal;
            Color = Color4.Black;
            TexCoord = texcoord;
        }

        public Vertex(in Vector3 position, in Vector3 normal, in Color4 color, in Vector2 texcoord)
        {
            Position = position;
            Normal = normal;
            Color = color;
            TexCoord = texcoord;
        }

        public readonly override bool Equals(object? obj) => obj is Vertex vertex && Equals(vertex);

        public readonly bool Equals(Vertex other) => Position.Equals(other.Position) &&
                                                     Normal.Equals(other.Normal) &&
                                                     Color.Equals(other.Color) &&
                                                     TexCoord.Equals(other.TexCoord);

        public readonly override int GetHashCode() => HashCode.Combine(Position, Normal, Color, TexCoord);

        public static bool operator ==(in Vertex left, in Vertex right) => left.Equals(right);

        public static bool operator !=(in Vertex left, in Vertex right) => !(left == right);
    }
}
