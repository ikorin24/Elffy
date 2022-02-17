#nullable enable
using OpenTK.Graphics.OpenGL4;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Elffy.Graphics.OpenGL
{
    [DebuggerDisplay("{DebuggerView,nq}")]
    public readonly struct Ssbo : IEquatable<Ssbo>
    {
        private readonly int _ssbo;

        internal int Value => _ssbo;

        public bool IsEmpty => _ssbo == Consts.NULL;

        public static Ssbo Empty => default;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebuggerView => IsEmpty ? "SSBO (null)" : $"SSBO={_ssbo}";

        [Obsolete("Don't use defaut constructor.", true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Ssbo() => throw new NotSupportedException("Don't use defaut constructor.");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Ssbo(int ssbo)
        {
            _ssbo = ssbo;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Ssbo Create()
        {
            GLAssert.EnsureContext();
            return new Ssbo(GL.GenBuffer());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Delete(ref Ssbo ssbo)
        {
            if(ssbo._ssbo != Consts.NULL) {
                GLAssert.EnsureContext();
                GL.DeleteBuffer(ssbo._ssbo);
                ssbo = default;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Bind(Ssbo ssbo)
        {
            GLAssert.EnsureContext();
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, ssbo._ssbo);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Unbind()
        {
            Bind(Empty);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void BufferData<T>(ReadOnlySpan<T> data, BufferHint hint) where T : unmanaged
        {
            fixed(T* ptr = data) {
                BufferData(data.Length * sizeof(T), ptr, hint);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void BufferData(int byteSize, void* data, BufferHint hint)
        {
            GLAssert.EnsureContext();
            GL.BufferData(BufferTarget.ShaderStorageBuffer, byteSize, (IntPtr)data, hint.ToOriginalValue());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void BindBase(Ssbo ssbo, int index)
        {
            GLAssert.EnsureContext();
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, index, ssbo._ssbo);
        }

        public override bool Equals(object? obj) => obj is Ssbo ssbo && Equals(ssbo);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Ssbo other) => _ssbo == other._ssbo;

        public override int GetHashCode() => _ssbo.GetHashCode();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Ssbo left, Ssbo right) => left.Equals(right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Ssbo left, Ssbo right) => !(left == right);
    }
}
