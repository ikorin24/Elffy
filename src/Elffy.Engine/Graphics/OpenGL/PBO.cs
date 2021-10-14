#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using OpenTK.Graphics.OpenGL4;

namespace Elffy.Graphics.OpenGL
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
        public static void Bind(in PBO pbo, BufferPackTarget target)
        {
            GLAssert.EnsureContext();
            GL.BindBuffer(target.Compat(), pbo._pbo);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Unbind(BufferPackTarget target)
        {
            GLAssert.EnsureContext();
            GL.BindBuffer(target.Compat(), Consts.NULL);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void BufferData(BufferPackTarget target, int size, IntPtr ptr, BufferUsage usage)
        {
            GLAssert.EnsureContext();
            GL.BufferData(target.Compat(), size, ptr, usage.Compat());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe T* MapBuffer<T>(BufferPackTarget target, BufferAccessMode access) where T : unmanaged
        {
            GLAssert.EnsureContext();
            return (T*)GL.MapBuffer(target.Compat(), (BufferAccess)access);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void UnmapBuffer(BufferPackTarget target)
        {
            GLAssert.EnsureContext();
            GL.UnmapBuffer(target.Compat());
        }

        public override bool Equals(object? obj) => obj is PBO pbo && Equals(pbo);

        public bool Equals(PBO other) => _pbo == other._pbo;

        public override int GetHashCode() => _pbo.GetHashCode();
    }
}
