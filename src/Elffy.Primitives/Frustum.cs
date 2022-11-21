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

        public bool Contains(in Vector3 pos)
        {
            var clips = CalcClipNormals(stackalloc (Vector3, float)[6]);
            return IsInsideClips(pos, clips);
        }

        [SkipLocalsInit]
        public bool Intersect(in Bounds bounds)
        {
            Span<Vector3> boundsCorners = stackalloc Vector3[CornerCount];
            bounds.GetCorners(boundsCorners);
            var clips = CalcClipNormals(stackalloc (Vector3, float)[6]);
            return
                IsInsideClips(boundsCorners[7], clips) ||
                IsInsideClips(boundsCorners[0], clips) ||
                IsInsideClips(boundsCorners[1], clips) ||
                IsInsideClips(boundsCorners[2], clips) ||
                IsInsideClips(boundsCorners[3], clips) ||
                IsInsideClips(boundsCorners[4], clips) ||
                IsInsideClips(boundsCorners[5], clips) ||
                IsInsideClips(boundsCorners[6], clips);
        }

        private ReadOnlySpan<(Vector3 Normal, float D)> CalcClipNormals(Span<(Vector3 Normal, float D)> clips)
        {
            // The normal is oriented toward the inside of the frustum.
            clips[0].Normal = Vector3.Cross(NearRightTop - NearLeftTop, NearLeftBottom - NearLeftTop).Normalized();        // near
            clips[1].Normal = Vector3.Cross(FarLeftBottom - FarLeftTop, FarRightTop - FarLeftTop).Normalized();            // far
            clips[2].Normal = Vector3.Cross(NearLeftTop - FarLeftTop, FarLeftBottom - FarLeftTop).Normalized();            // left
            clips[3].Normal = Vector3.Cross(FarRightTop - NearRightTop, NearRightBottom - NearRightTop).Normalized();      // right
            clips[4].Normal = Vector3.Cross(FarLeftTop - NearLeftTop, NearRightTop - NearLeftTop).Normalized();            // top
            clips[5].Normal = Vector3.Cross(NearRightBottom - NearLeftBottom, FarLeftBottom - NearLeftBottom).Normalized();// bottom

            clips[0].D = clips[0].Normal.Dot(NearLeftTop);
            clips[1].D = clips[1].Normal.Dot(FarLeftTop);
            clips[2].D = clips[2].Normal.Dot(FarLeftTop);
            clips[3].D = clips[3].Normal.Dot(NearRightTop);
            clips[4].D = clips[4].Normal.Dot(NearLeftTop);
            clips[5].D = clips[5].Normal.Dot(NearLeftBottom);

            return clips;
        }

        private static bool IsInsideClips(in Vector3 p, ReadOnlySpan<(Vector3 Normal, float D)> clips)
        {
            // 'clips[i].Normal.Dot(p) - clips[i].D' is distance from a clip.
            // distance >= 0 means p is inside of the frustum
            return
                (clips[5].Normal.Dot(p) - clips[5].D >= 0) &&
                (clips[0].Normal.Dot(p) - clips[0].D >= 0) &&
                (clips[1].Normal.Dot(p) - clips[1].D >= 0) &&
                (clips[2].Normal.Dot(p) - clips[2].D >= 0) &&
                (clips[3].Normal.Dot(p) - clips[3].D >= 0) &&
                (clips[4].Normal.Dot(p) - clips[4].D >= 0);
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


        //private readonly struct Plain
        //{
        //    // [equation of plain]
        //    // nx*x + ny*y + nz*z - d = 0

        //    public readonly Vector3 Normal; // normalized
        //    private readonly float _d;

        //    private Plain(Vector3 normal, float d)
        //    {
        //        Normal = normal;
        //        _d = d;
        //    }

        //    public static Plain FromTriangle(in Vector3 p0, in Vector3 p1, in Vector3 p2)
        //    {
        //        var n = Vector3.Cross(p2 - p1, p0 - p1).Normalized();
        //        return new Plain(n, n.Dot(p1));
        //    }

        //    public float GetSignedDistance(in Vector3 pos) => Normal.Dot(pos) - _d;

        //    public float GetDistance(in Vector3 pos) => MathF.Abs(Normal.Dot(pos) - _d);
        //}
    }
}
