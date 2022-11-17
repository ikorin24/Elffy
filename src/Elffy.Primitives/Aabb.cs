#nullable enable
using System;

namespace Elffy
{
    public readonly struct Aabb : IEquatable<Aabb>
    {
        public readonly Vector3 Min;
        public readonly Vector3 Max;

        public static Aabb None => default;

        public Aabb(in Vector3 min, in Vector3 max)
        {
            Min = min;
            Max = max;
        }

        public static Aabb CreateMeshAabb<TVertex>(ReadOnlySpan<TVertex> vertices, ReadOnlySpan<uint> indices) where TVertex : unmanaged, IVertex
        {
            unsafe {
                fixed(TVertex* vp = vertices) {
                    fixed(uint* ip = indices) {
                        return CreateMeshAabb(vp, (ulong)vertices.Length, ip, (uint)indices.Length);
                    }
                }
            }
        }

        public unsafe static Aabb CreateMeshAabb<TVertex>(TVertex* vertices, ulong vertexCount, uint* indices, uint indexCount) where TVertex : unmanaged, IVertex
        {
            var min = Vector3.Zero;
            var max = Vector3.Zero;
            if(TVertex.TryGetPositionAccessor(out var posAccessor)) {
                for(uint i = 0; i < indexCount; i++) {
                    ref readonly var pos = ref posAccessor.Field(vertices[indices[i]]);
                    min.X = MathF.Min(min.X, pos.X);
                    min.Y = MathF.Min(min.Y, pos.Y);
                    min.Z = MathF.Min(min.Z, pos.Z);
                    max.X = MathF.Max(max.X, pos.X);
                    max.Y = MathF.Max(max.Y, pos.Y);
                    max.Z = MathF.Max(max.Z, pos.Z);
                }
            }
            return new Aabb(min, max);
        }

        public override bool Equals(object? obj) => obj is Aabb aabb && Equals(aabb);

        public bool Equals(Aabb other) => Min.Equals(other.Min) && Max.Equals(other.Max);

        public override int GetHashCode() => HashCode.Combine(Min, Max);

        public static bool operator ==(Aabb left, Aabb right) => left.Equals(right);

        public static bool operator !=(Aabb left, Aabb right) => !(left == right);
    }
}
