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
        public Vector2 UV;

        static Vertex()
        {
            VertexMarshalHelper.Register<Vertex>(
                fieldName => fieldName switch
            {
                nameof(Position) => (0, VertexFieldMarshalType.Float, 3),
                nameof(Normal) => (12, VertexFieldMarshalType.Float, 3),
                nameof(UV) => (24, VertexFieldMarshalType.Float, 2),
                _ => throw new ArgumentException(),
            },
                specialField => specialField switch
            {
                VertexSpecialField.Position => nameof(Position),
                VertexSpecialField.Normal => nameof(Normal),
                VertexSpecialField.UV => nameof(UV),
                _ => "",
            });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vertex(in Vector3 position, in Vector3 normal, in Vector2 uv)
        {
            Position = position;
            Normal = normal;
            UV = uv;
        }

        public readonly override bool Equals(object? obj) => obj is Vertex vertex && Equals(vertex);

        public readonly bool Equals(Vertex other) => Position.Equals(other.Position) &&
                                                     Normal.Equals(other.Normal) &&
                                                     UV.Equals(other.UV);

        public readonly override int GetHashCode() => HashCode.Combine(Position, Normal, UV);

        public static bool operator ==(in Vertex left, in Vertex right) => left.Equals(right);

        public static bool operator !=(in Vertex left, in Vertex right) => !(left == right);
    }
}
