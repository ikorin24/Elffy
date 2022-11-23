#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Elffy
{
    public readonly struct Frustum : IEquatable<Frustum>
    {
        // Don't change fields order.
        public readonly Vector3 NearLeftBottom;
        public readonly Vector3 NearLeftTop;
        public readonly Vector3 NearRightBottom;
        public readonly Vector3 NearRightTop;
        public readonly Vector3 FarLeftBottom;
        public readonly Vector3 FarLeftTop;
        public readonly Vector3 FarRightBottom;
        public readonly Vector3 FarRightTop;
        public readonly PlainEquation NearClip;
        public readonly PlainEquation FarClip;
        public readonly PlainEquation LeftClip;
        public readonly PlainEquation RightClip;
        public readonly PlainEquation TopClip;
        public readonly PlainEquation BottomClip;

        public Vector3 NearCenterBottom => (NearLeftBottom + NearRightBottom) * 0.5f;
        public Vector3 NearCenterTop => (NearLeftTop + NearRightTop) * 0.5f;
        public Vector3 NearLeftCenter => (NearLeftBottom + NearLeftTop) * 0.5f;
        public Vector3 NearRightCenter => (NearRightBottom + NearRightTop) * 0.5f;

        public Vector3 FarCenterBottom => (FarLeftBottom + FarRightBottom) * 0.5f;
        public Vector3 FarCenterTop => (FarLeftTop + FarRightTop) * 0.5f;
        public Vector3 FarLeftCenter => (FarLeftBottom + FarLeftTop) * 0.5f;
        public Vector3 FarRightCenter => (FarRightBottom + FarRightTop) * 0.5f;

        public Vector3 CenterLeftBottom => (NearLeftBottom + FarLeftBottom) * 0.5f;
        public Vector3 CenterLeftTop => (NearLeftTop + FarLeftTop) * 0.5f;
        public Vector3 CenterRightBottom => (NearRightBottom + FarRightBottom) * 0.5f;
        public Vector3 CenterRightTop => (NearRightTop + FarRightTop) * 0.5f;

        public Vector3 NearPlainCenter => (NearLeftBottom + NearLeftTop + NearRightBottom + NearRightTop) * 0.25f;
        public Vector3 FarPlainCenter => (FarLeftBottom + FarLeftTop + FarRightBottom + FarRightTop) * 0.25f;
        public Vector3 LeftPlainCenter => (NearLeftBottom + NearLeftTop + FarLeftBottom + FarLeftTop) * 0.25f;
        public Vector3 RightPlainCenter => (NearRightBottom + NearRightTop + FarRightBottom + FarRightTop) * 0.25f;
        public Vector3 BottomPlainCenter => (NearLeftBottom + NearRightBottom + FarLeftBottom + FarRightBottom) * 0.25f;
        public Vector3 TopPlainCenter => (NearLeftTop + NearRightTop + FarLeftTop + FarRightTop) * 0.25f;

        public Vector3 Center => (NearLeftBottom + NearLeftTop + NearRightBottom + NearRightTop + FarLeftBottom + FarLeftTop + FarRightBottom + FarRightTop) * 0.125f;

        public ReadOnlySpan<Vector3> Corners
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in NearLeftBottom), 8);
        }

        public ReadOnlySpan<PlainEquation> ClipPlains
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in NearClip), 6);
        }

        private Frustum(
            in Vector3 nearLeftBottom,
            in Vector3 nearLeftTop,
            in Vector3 nearRightBottom,
            in Vector3 nearRightTop,
            in Vector3 farLeftBottom,
            in Vector3 farLeftTop,
            in Vector3 farRightBottom,
            in Vector3 farRightTop
        )
        {
            NearLeftBottom = nearLeftBottom;
            NearLeftTop = nearLeftTop;
            NearRightBottom = nearRightBottom;
            NearRightTop = nearRightTop;
            FarLeftBottom = farLeftBottom;
            FarLeftTop = farLeftTop;
            FarRightBottom = farRightBottom;
            FarRightTop = farRightTop;

            NearClip = PlainEquation.FromTriangle(NearLeftBottom, NearLeftTop, NearRightTop);
            FarClip = PlainEquation.FromTriangle(FarRightTop, FarLeftTop, FarLeftBottom);
            LeftClip = PlainEquation.FromTriangle(FarLeftBottom, FarLeftTop, NearLeftTop);
            RightClip = PlainEquation.FromTriangle(NearRightBottom, NearRightTop, FarRightTop);
            TopClip = PlainEquation.FromTriangle(NearRightTop, NearLeftTop, FarLeftTop);
            BottomClip = PlainEquation.FromTriangle(FarLeftBottom, NearLeftBottom, NearRightBottom);
        }

        [SkipLocalsInit]
        public bool Intersect(in Bounds bounds)
        {
            Span<Vector3> boundsCorners = stackalloc Vector3[8];
            bounds.GetCorners(boundsCorners);
            if(ContainsAny(in this, boundsCorners)) {
                return true;
            }

            // TODO:
            //var clips = ClipPlains;
            return false;
        }

        public unsafe bool Contains(in Vector3 p)
        {
            return ContainsAny(in this, new ReadOnlySpan<Vector3>(in p));
        }

        private static unsafe bool ContainsAny(in Frustum frustum, ReadOnlySpan<Vector3> positions)
        {
            // [NOTE]
            // Layout of PlainEquation is (float nx, float ny, float nz, float d).
            // So I consider it as Vector4 (float x, float y, float z, float w).

            if(Avx.IsSupported) {
                fixed(PlainEquation* clips = frustum.ClipPlains) {
                    Vector4* c = (Vector4*)clips;
                    var clip01 = Vector256.Load((float*)&c[0]);      // <c0x, c0y, c0z, c0w, c1x, c1y, c1z, c1w>
                    var clip23 = Vector256.Load((float*)&c[2]);      // <c2x, c2y, c2z, c2w, c3x, c3y, c3z, c3w>
                    var clip45 = Vector256.Load((float*)&c[4]);      // <c4x, c4y, c4z, c4w, c5x, c5y, c5z, c5w>

                    foreach(var p in positions) {
                        var a = Vector256.Create(p.X, p.Y, p.Z, 1f, p.X, p.Y, p.Z, 1f);     // <px, py, pz, pw, px, py, pz, pw>  (pw == 1)
                        var result0 = Avx.DotProduct(clip01, a, 0b_1111_0001);  // <dot(c0, p), 0, 0, 0, dot(c1, p), 0, 0, 0>
                        var result1 = Avx.DotProduct(clip23, a, 0b_1111_0001);  // <dot(c2, p), 0, 0, 0, dot(c3, p), 0, 0, 0>
                        var result2 = Avx.DotProduct(clip45, a, 0b_1111_0001);  // <dot(c4, p), 0, 0, 0, dot(c5, p), 0, 0, 0>

                        var contains =
                            result0[0] >= 0 &&      // dot(c0, p) >= 0
                            result0[4] >= 0 &&      // dot(c1, p) >= 0
                            result1[0] >= 0 &&      // dot(c2, p) >= 0
                            result1[4] >= 0 &&      // dot(c3, p) >= 0
                            result2[0] >= 0 &&      // dot(c4, p) >= 0
                            result2[4] >= 0;        // dot(c5, p) >= 0
                        if(contains) {
                            return true;
                        }
                    }
                    return false;
                }
            }

            return SoftwareFallback(in frustum, positions);

            static bool SoftwareFallback(in Frustum frustum, ReadOnlySpan<Vector3> positions)
            {
                foreach(var p in positions) {
                    var contains =
                        frustum.NearClip.IsAbove(p) &&
                        frustum.FarClip.IsAbove(p) &&
                        frustum.LeftClip.IsAbove(p) &&
                        frustum.RightClip.IsAbove(p) &&
                        frustum.TopClip.IsAbove(p) &&
                        frustum.BottomClip.IsAbove(p);
                    if(contains) {
                        return true;
                    }
                }
                return false;
            }
        }

        public static void FromMatrix(in Matrix4 projection, in Matrix4 view, out Frustum frustum)
        {
            var viewProjInv = (projection * view).Inverted();
            frustum = new(
                nearLeftBottom: viewProjInv.Transform(-1, -1, -1),
                nearLeftTop: viewProjInv.Transform(-1, 1, -1),
                nearRightBottom: viewProjInv.Transform(1, -1, -1),
                nearRightTop: viewProjInv.Transform(1, 1, -1),
                farLeftBottom: viewProjInv.Transform(-1, -1, 1),
                farLeftTop: viewProjInv.Transform(-1, 1, 1),
                farRightBottom: viewProjInv.Transform(1, -1, 1),
                farRightTop: viewProjInv.Transform(1, 1, 1)
            );
        }

        public override bool Equals(object? obj) => obj is Frustum frustum && Equals(frustum);

        public bool Equals(Frustum other)
        {
            return NearLeftBottom.Equals(other.NearLeftBottom) &&
                   NearLeftTop.Equals(other.NearLeftTop) &&
                   NearRightBottom.Equals(other.NearRightBottom) &&
                   NearRightTop.Equals(other.NearRightTop) &&
                   FarLeftBottom.Equals(other.FarLeftBottom) &&
                   FarLeftTop.Equals(other.FarLeftTop) &&
                   FarRightBottom.Equals(other.FarRightBottom) &&
                   FarRightTop.Equals(other.FarRightTop) &&
                   NearClip.Equals(other.NearClip) &&
                   FarClip.Equals(other.FarClip) &&
                   LeftClip.Equals(other.LeftClip) &&
                   RightClip.Equals(other.RightClip) &&
                   TopClip.Equals(other.TopClip) &&
                   BottomClip.Equals(other.BottomClip);
        }

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(NearLeftBottom);
            hash.Add(NearLeftTop);
            hash.Add(NearRightBottom);
            hash.Add(NearRightTop);
            hash.Add(FarLeftBottom);
            hash.Add(FarLeftTop);
            hash.Add(FarRightBottom);
            hash.Add(FarRightTop);
            hash.Add(NearClip);
            hash.Add(FarClip);
            hash.Add(LeftClip);
            hash.Add(RightClip);
            hash.Add(TopClip);
            hash.Add(BottomClip);
            return hash.ToHashCode();
        }
    }
}
