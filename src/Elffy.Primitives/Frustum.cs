#nullable enable
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Elffy
{
    public readonly struct Frustum : IEquatable<Frustum>
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private const int CornerCount = 8;

        public required Vector3 NearLeftBottom { get; init; }
        public required Vector3 NearLeftTop { get; init; }
        public required Vector3 NearRightBottom { get; init; }
        public required Vector3 NearRightTop { get; init; }
        public required Vector3 FarLeftBottom { get; init; }
        public required Vector3 FarLeftTop { get; init; }
        public required Vector3 FarRightBottom { get; init; }
        public required Vector3 FarRightTop { get; init; }

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
                   FarRightTop.Equals(other.FarRightTop);
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            ref var r = ref Unsafe.As<Frustum, byte>(ref Unsafe.AsRef(in this));
            var bytes = MemoryMarshal.CreateReadOnlySpan(ref r, Unsafe.SizeOf<Frustum>());
            hashCode.AddBytes(bytes);
            return hashCode.ToHashCode();
        }
    }
}
