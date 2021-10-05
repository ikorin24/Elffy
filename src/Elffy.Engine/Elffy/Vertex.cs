#nullable enable
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.ComponentModel;

namespace Elffy
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

        [ModuleInitializer]
        [EditorBrowsable(EditorBrowsableState.Never)]
        internal static void RegisterVertexTypeDataOnModuleInitialized()
        {
            VertexMarshalHelper.Register<Vertex>(new[]
            {
                new VertexFieldData(nameof(Position), typeof(Vector3), VertexSpecialField.Position, 0, VertexFieldMarshalType.Float, 3),
                new VertexFieldData(nameof(Normal), typeof(Vector3), VertexSpecialField.Normal, 12, VertexFieldMarshalType.Float, 3),
                new VertexFieldData(nameof(UV), typeof(Vector2), VertexSpecialField.UV, 24, VertexFieldMarshalType.Float, 2),

            }).ThrowIfError();
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
