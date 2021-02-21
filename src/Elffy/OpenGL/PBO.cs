#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using OpenTK.Graphics.OpenGL4;
using Elffy.Core;

namespace Elffy.OpenGL
{
    [DebuggerDisplay("PBO={_pbo}")]
    public readonly struct PBO : IEquatable<PBO>
    {
        private readonly int _pbo;

        internal int Value => _pbo;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private PBO(int pbo)
        {
            _pbo = pbo;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PBO Create()
        {
            GLAssert.EnsureContext();
            return new PBO(GL.GenBuffer());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Delete(ref PBO pbo)
        {
            if(pbo._pbo != Consts.NULL) {
                GL.DeleteBuffer(pbo._pbo);
                pbo = default;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Bind(in PBO pbo)
        {
            GLAssert.EnsureContext();
            GL.BindBuffer(BufferTarget.PixelUnpackBuffer, pbo._pbo);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Unbind()
        {
            GLAssert.EnsureContext();
            GL.BindBuffer(BufferTarget.PixelUnpackBuffer, Consts.NULL);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void BufferData(in PBO pbo, int size, IntPtr ptr BufferUsage usage)
        {
            GLAssert.EnsureContext();
            GL.BufferData(BufferTarget.PixelUnpackBuffer, size, ptr, usage.ToBufferUsageHint());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe T* MapBuffer<T>(BufferAccess access) where T : unmanaged
        {
            GLAssert.EnsureContext();
            return (T*)GL.MapBuffer(BufferTarget.PixelUnpackBuffer, access);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void UnmapBuffer()
        {
            GLAssert.EnsureContext();
            GL.UnmapBuffer(BufferTarget.PixelUnpackBuffer);
        }

        public override bool Equals(object? obj) => obj is PBO pBO && Equals(pBO);

        public bool Equals(PBO other) => _pbo == other._pbo;

        public override int GetHashCode() => _pbo.GetHashCode();
    }
}
