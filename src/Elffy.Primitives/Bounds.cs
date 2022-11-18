#nullable enable
using System;
using System.Runtime.CompilerServices;

namespace Elffy
{
    public readonly struct Bounds : IEquatable<Bounds>
    {
        //public readonly Vector3 Min;
        //public readonly Vector3 Max;
        //public Vector3 Center => (Min + Max) * 0.5f;
        //public Vector3 Size => Max - Min;
        //public Vector3 Extents => (Max - Min) * 0.5f;

        //public Bounds(in Vector3 min, in Vector3 max)
        //{
        //    Min = min;
        //    Max = max;
        //}
        //public static Bounds FromMinMax(in Vector3 min, in Vector3 max) => new Bounds(min, max);
        //public static Bounds FromCenterExtents(in Vector3 center, in Vector3 extents) => new Bounds(center - extents, center + extents);

        public readonly Vector3 Center;
        public readonly Vector3 Extents;
        public Vector3 Min => Center - Extents;
        public Vector3 Max => Center + Extents;
        public Vector3 Size => Extents * 2;
        private Bounds(in Vector3 center, in Vector3 extents)
        {
            Center = center;
            Extents = extents;
        }
        public static Bounds FromMinMax(in Vector3 min, in Vector3 max) => new Bounds((min + max) * 0.5f, (max - min) * 0.5f);
        public static Bounds FromCenterExtents(in Vector3 center, in Vector3 extents) => new Bounds(center, extents);


        public static Bounds None => default;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Bounds TransformedBy(in Matrix4 transform, bool isMatrix4x3 = false)
        {
            return isMatrix4x3 ?
                FromMinMax(transform.TransformFast4x3(Min), transform.TransformFast4x3(Max)) :
                FromMinMax(transform.Transform(Min), transform.Transform(Max));
        }

        public int GetCorners(Span<Vector3> corners)
        {
            if(corners.Length < 8) {
                throw new ArgumentException(nameof(corners));
            }
            var (ex, ey, ez) = Extents;
            corners[0] = Center + new Vector3(ex, ey, ez);
            corners[1] = Center + new Vector3(ex, ey, -ez);
            corners[2] = Center + new Vector3(ex, -ey, ez);
            corners[3] = Center + new Vector3(ex, -ey, -ez);
            corners[4] = Center + new Vector3(-ex, ey, ez);
            corners[5] = Center + new Vector3(-ex, ey, -ez);
            corners[6] = Center + new Vector3(-ex, -ey, ez);
            corners[7] = Center + new Vector3(-ex, -ey, -ez);
            return 8;
        }

        public static Bounds CreateMeshAabb<TVertex>(ReadOnlySpan<TVertex> vertices, ReadOnlySpan<uint> indices) where TVertex : unmanaged, IVertex
        {
            unsafe {
                fixed(TVertex* vp = vertices) {
                    fixed(uint* ip = indices) {
                        return CreateMeshAabb(vp, (ulong)vertices.Length, ip, (uint)indices.Length);
                    }
                }
            }
        }

        public unsafe static Bounds CreateMeshAabb<TVertex>(TVertex* vertices, ulong vertexCount, uint* indices, uint indexCount) where TVertex : unmanaged, IVertex
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
            return FromMinMax(min, max);
        }

        public override bool Equals(object? obj) => obj is Bounds aabb && Equals(aabb);

        public bool Equals(Bounds other) => Min.Equals(other.Min) && Max.Equals(other.Max);

        public override int GetHashCode() => HashCode.Combine(Min, Max);

        public static bool operator ==(Bounds left, Bounds right) => left.Equals(right);

        public static bool operator !=(Bounds left, Bounds right) => !(left == right);
    }
}
