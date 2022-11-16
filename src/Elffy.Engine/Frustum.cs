#nullable enable
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Elffy
{
    public struct Frustum : IEquatable<Frustum>
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private const int CornerCount = 8;

        public Vector3 NearLeftBottom;
        public Vector3 NearLeftTop;
        public Vector3 NearRightBottom;
        public Vector3 NearRightTop;
        public Vector3 FarLeftBottom;
        public Vector3 FarLeftTop;
        public Vector3 FarRightBottom;
        public Vector3 FarRightTop;

        public readonly Vector3 NearCenterBottom => (NearLeftBottom + NearRightBottom) * 0.5f;
        public readonly Vector3 NearCenterTop => (NearLeftTop + NearRightTop) * 0.5f;
        public readonly Vector3 NearLeftCenter => (NearLeftBottom + NearLeftTop) * 0.5f;
        public readonly Vector3 NearRightCenter => (NearRightBottom + NearRightTop) * 0.5f;

        public readonly Vector3 FarCenterBottom => (FarLeftBottom + FarRightBottom) * 0.5f;
        public readonly Vector3 FarCenterTop => (FarLeftTop + FarRightTop) * 0.5f;
        public readonly Vector3 FarLeftCenter => (FarLeftBottom + FarLeftTop) * 0.5f;
        public readonly Vector3 FarRightCenter => (FarRightBottom + FarRightTop) * 0.5f;

        public readonly Vector3 CenterLeftBottom => (NearLeftBottom + FarLeftBottom) * 0.5f;
        public readonly Vector3 CenterLeftTop => (NearLeftTop + FarLeftTop) * 0.5f;
        public readonly Vector3 CenterRightBottom => (NearRightBottom + FarRightBottom) * 0.5f;
        public readonly Vector3 CenterRightTop => (NearRightTop + FarRightTop) * 0.5f;

        public readonly Vector3 NearPlainCenter => (NearLeftBottom + NearLeftTop + NearRightBottom + NearRightTop) * 0.25f;
        public readonly Vector3 FarPlainCenter => (FarLeftBottom + FarLeftTop + FarRightBottom + FarRightTop) * 0.25f;
        public readonly Vector3 LeftPlainCenter => (NearLeftBottom + NearLeftTop + FarLeftBottom + FarLeftTop) * 0.25f;
        public readonly Vector3 RightPlainCenter => (NearRightBottom + NearRightTop + FarRightBottom + FarRightTop) * 0.25f;
        public readonly Vector3 BottomPlainCenter => (NearLeftBottom + NearRightBottom + FarLeftBottom + FarRightBottom) * 0.25f;
        public readonly Vector3 TopPlainCenter => (NearLeftTop + NearRightTop + FarLeftTop + FarRightTop) * 0.25f;

        public readonly Vector3 Center => (NearLeftBottom + NearLeftTop + NearRightBottom + NearRightTop + FarLeftBottom + FarLeftTop + FarRightBottom + FarRightTop) * 0.125f;

        public readonly ReadOnlySpan<Vector3> Corners
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<Frustum, Vector3>(ref Unsafe.AsRef(in this)), CornerCount);
        }

        public static Frustum FromMatrix(in Matrix4 projection, in Matrix4 view)
        {
            FromMatrix(projection, view, out var frustum);
            return frustum;
        }

        public static void FromMatrix(in Matrix4 projection, in Matrix4 view, out Frustum frustum)
        {
            var viewProjInv = (projection * view).Inverted();
            frustum = new()
            {
                NearLeftBottom = viewProjInv.Transform(-1, -1, -1),
                NearLeftTop = viewProjInv.Transform(-1, 1, -1),
                NearRightBottom = viewProjInv.Transform(1, -1, -1),
                NearRightTop = viewProjInv.Transform(1, 1, -1),
                FarLeftBottom = viewProjInv.Transform(-1, -1, 1),
                FarLeftTop = viewProjInv.Transform(-1, 1, 1),
                FarRightBottom = viewProjInv.Transform(1, -1, 1),
                FarRightTop = viewProjInv.Transform(1, 1, 1),
            };
        }

        public readonly override bool Equals(object? obj) => obj is Frustum frustum && Equals(frustum);

        public readonly bool Equals(Frustum other)
        {
            return NearLeftBottom.Equals(other.NearLeftBottom) &&
                   NearLeftTop.Equals(other.NearLeftTop) &&
                   NearRightBottom.Equals(other.NearRightBottom) &&
                   NearRightTop.Equals(other.NearRightTop) &&
                   FarLeftBottom.Equals(other.FarLeftBottom) &&
                   FarLeftTop.Equals(other.FarLeftTop) &&
                   FarRightBottom.Equals(other.FarRightBottom) &&
                   FarRightTop.Equals(other.FarRightTop);
        }

        public readonly override int GetHashCode()
        {
            var hashCode = new HashCode();
            ref var r = ref Unsafe.As<Frustum, byte>(ref Unsafe.AsRef(in this));
            var bytes = MemoryMarshal.CreateReadOnlySpan(ref r, Unsafe.SizeOf<Frustum>());
            hashCode.AddBytes(bytes);
            return hashCode.ToHashCode();
        }
    }
}
