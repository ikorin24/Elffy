#nullable enable
using Elffy.Diagnostics;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Elffy.Core
{
    /// <summary>Slim vertex struct, which has position and uv.</summary>
    /// <remarks>If you need "Normal", use <see cref="Vertex"/> instead.</remarks>
    [DebuggerDisplay("{Position}")]
    [StructLayout(LayoutKind.Explicit)]
    [VertexLike]
    public unsafe struct VertexSlim : IEquatable<VertexSlim>
    {
        [FieldOffset(0)]
        public Vector3 Position;
        [FieldOffset(12)]
        public Vector2 UV;

        static VertexSlim()
        {
            // Register struct layout cache
            VertexMarshalHelper<VertexSlim>.Register(fieldName => fieldName switch
            {
                nameof(Position) => (0, VertexFieldMarshalType.Float, 3),
                nameof(UV) => (12, VertexFieldMarshalType.Float, 2),
                _ => throw new ArgumentException(),
            });
        }

        public VertexSlim(in Vector3 position, in Vector2 uv)
        {
            Position = position;
            UV = uv;
        }

        public readonly override bool Equals(object? obj) => obj is VertexSlim slim && Equals(slim);

        public readonly bool Equals(VertexSlim other) => Position.Equals(other.Position) &&
                                                         UV.Equals(other.UV);

        public readonly override int GetHashCode() => HashCode.Combine(Position, UV);

        public static bool operator ==(in VertexSlim left, in VertexSlim right) => left.Equals(right);

        public static bool operator !=(in VertexSlim left, in VertexSlim right) => !(left == right);
    }
}
