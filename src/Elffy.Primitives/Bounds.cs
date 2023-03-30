#nullable enable
using System;
using System.Collections.Generic;

namespace Elffy
{
    public readonly struct Bounds : IEquatable<Bounds>
    {
        public readonly Vector3 Center;
        public readonly Vector3 Extents;

        public Vector3 Min => Center - Extents;
        public Vector3 Max => Center + Extents;
        public Vector3 Size => Extents * 2;

        public static Bounds None => default;

        private Bounds(in Vector3 center, in Vector3 extents)
        {
            Center = center;
            Extents = extents;
        }

        public Bounds ChangeCoordinate(in Matrix4 transform, bool isMatrix4x3 = false)
        {
            var (ex, ey, ez) = Extents;
            ReadOnlySpan<Vector3> corners = isMatrix4x3 ? stackalloc Vector3[8]
            {
                transform.TransformFast4x3(Center + new Vector3(ex, ey, ez)),
                transform.TransformFast4x3(Center + new Vector3(ex, ey, -ez)),
                transform.TransformFast4x3(Center + new Vector3(ex, -ey, ez)),
                transform.TransformFast4x3(Center + new Vector3(ex, -ey, -ez)),
                transform.TransformFast4x3(Center + new Vector3(-ex, ey, ez)),
                transform.TransformFast4x3(Center + new Vector3(-ex, ey, -ez)),
                transform.TransformFast4x3(Center + new Vector3(-ex, -ey, ez)),
                transform.TransformFast4x3(Center + new Vector3(-ex, -ey, -ez)),
            } : stackalloc Vector3[8]
            {
                transform.Transform(Center + new Vector3(ex, ey, ez)),
                transform.Transform(Center + new Vector3(ex, ey, -ez)),
                transform.Transform(Center + new Vector3(ex, -ey, ez)),
                transform.Transform(Center + new Vector3(ex, -ey, -ez)),
                transform.Transform(Center + new Vector3(-ex, ey, ez)),
                transform.Transform(Center + new Vector3(-ex, ey, -ez)),
                transform.Transform(Center + new Vector3(-ex, -ey, ez)),
                transform.Transform(Center + new Vector3(-ex, -ey, -ez)),
            };
            var (min, max) = (corners[0], corners[0]);
            (min, max) = (Vector3.Min(min, corners[1]), Vector3.Max(max, corners[1]));
            (min, max) = (Vector3.Min(min, corners[2]), Vector3.Max(max, corners[2]));
            (min, max) = (Vector3.Min(min, corners[3]), Vector3.Max(max, corners[3]));
            (min, max) = (Vector3.Min(min, corners[4]), Vector3.Max(max, corners[4]));
            (min, max) = (Vector3.Min(min, corners[5]), Vector3.Max(max, corners[5]));
            (min, max) = (Vector3.Min(min, corners[6]), Vector3.Max(max, corners[6]));
            (min, max) = (Vector3.Min(min, corners[7]), Vector3.Max(max, corners[7]));
            return FromMinMax(min, max);
        }

        public Vector3[] GetCorners()
        {
            var corners = new Vector3[8];
            GetCorners(corners);
            return corners;
        }

        public int GetCorners(Span<Vector3> corners)
        {
            if(corners.Length < 8) {
                ThrowArg();
                static void ThrowArg() => throw new ArgumentException($"{nameof(corners)} length should be 8 at least.", nameof(corners));
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

        public static Bounds FromMinMax(in Vector3 min, in Vector3 max) => new Bounds((min + max) * 0.5f, (max - min) * 0.5f);
        public static Bounds FromCenterExtents(in Vector3 center, in Vector3 extents) => new Bounds(center, extents);

        public static Bounds CreateAabb(IEnumerable<Vector3> points)
        {
            ArgumentNullException.ThrowIfNull(points);
            var min = Vector3.MaxValue;
            var max = Vector3.MinValue;
            foreach(var p in points) {
                min = Vector3.Min(min, p);
                max = Vector3.Max(max, p);
            }
            return FromMinMax(min, max);
        }

        public static Bounds CreateAabb(ReadOnlySpan<Vector3> points)
        {
            var min = Vector3.MaxValue;
            var max = Vector3.MinValue;
            foreach(var p in points) {
                min = Vector3.Min(min, p);
                max = Vector3.Max(max, p);
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
