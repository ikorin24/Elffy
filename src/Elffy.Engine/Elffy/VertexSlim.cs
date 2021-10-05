#nullable enable
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Elffy
{
    /// <summary>Slim vertex struct, which has position and uv.</summary>
    /// <remarks>If you need "Normal", use <see cref="Vertex"/> instead.</remarks>
    [DebuggerDisplay("{Position}")]
    [StructLayout(LayoutKind.Explicit)]
    [VertexLike]
    public struct VertexSlim : IEquatable<VertexSlim>
    {
        [FieldOffset(0)]
        public Vector3 Position;
        [FieldOffset(12)]
        public Vector2 UV;

        [ModuleInitializer]
        [EditorBrowsable(EditorBrowsableState.Never)]
        internal static void RegisterVertexTypeDataOnModuleInitialized()
        {
            VertexMarshalHelper.Register<VertexSlim>(new[]
            {
                new VertexFieldData(nameof(Position), typeof(Vector3), VertexSpecialField.Position, 0, VertexFieldMarshalType.Float, 3),
                new VertexFieldData(nameof(UV), typeof(Vector2), VertexSpecialField.UV, 12, VertexFieldMarshalType.Float, 2),

            }).ThrowIfError();
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
