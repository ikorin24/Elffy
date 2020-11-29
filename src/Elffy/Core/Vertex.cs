#nullable enable
using Elffy.Diagnostics;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace Elffy.Core
{
    [DebuggerDisplay("{Position}")]
    [VertexLike]
    [StructLayout(LayoutKind.Explicit)]
    public struct Vertex : IEquatable<Vertex>
    {
        [FieldOffset(0)]
        public Vector3 Position;
        [FieldOffset(12)]
        public Vector3 Normal;
        [FieldOffset(24)]
        public Vector2 TexCoord;

        static Vertex()
        {
            VertexMarshalHelper<Vertex>.Register(fieldName => fieldName switch
            {
                nameof(Position) => (0, VertexFieldMarshalType.Float, 3),
                nameof(Normal) =>   (12,   VertexFieldMarshalType.Float, 3),
                nameof(TexCoord) => (24, VertexFieldMarshalType.Float, 2),
                _ => throw new ArgumentException(),
            });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vertex(in Vector3 position, in Vector3 normal, in Vector2 texcoord)
        {
            Position = position;
            Normal = normal;
            TexCoord = texcoord;
        }

        public readonly override bool Equals(object? obj) => obj is Vertex vertex && Equals(vertex);

        public readonly bool Equals(Vertex other) => Position.Equals(other.Position) &&
                                                     Normal.Equals(other.Normal) &&
                                                     TexCoord.Equals(other.TexCoord);

        public readonly override int GetHashCode() => HashCode.Combine(Position, Normal, TexCoord);

        public static bool operator ==(in Vertex left, in Vertex right) => left.Equals(right);

        public static bool operator !=(in Vertex left, in Vertex right) => !(left == right);
    }
}
