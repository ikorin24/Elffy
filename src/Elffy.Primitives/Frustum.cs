#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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

        public bool Contains(in Vector3 pos)
        {
            return
                NearClip.GetSignedDistance(pos) >= 0 &&
                FarClip.GetSignedDistance(pos) >= 0 &&
                LeftClip.GetSignedDistance(pos) >= 0 &&
                RightClip.GetSignedDistance(pos) >= 0 &&
                TopClip.GetSignedDistance(pos) >= 0 &&
                BottomClip.GetSignedDistance(pos) >= 0;
        }

        [SkipLocalsInit]
        public bool Intersect(in Bounds bounds)
        {
            Span<Vector3> boundsCorners = stackalloc Vector3[8];
            bounds.GetCorners(boundsCorners);
            return
                Contains(boundsCorners[0]) &&
                Contains(boundsCorners[1]) &&
                Contains(boundsCorners[2]) &&
                Contains(boundsCorners[3]) &&
                Contains(boundsCorners[4]) &&
                Contains(boundsCorners[5]) &&
                Contains(boundsCorners[6]) &&
                Contains(boundsCorners[7]);
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
            HashCode hash = new HashCode();
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
