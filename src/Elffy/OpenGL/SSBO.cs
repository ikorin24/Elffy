#nullable enable
using Elffy.Core;
using OpenToolkit.Graphics.OpenGL;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Elffy.OpenGL
{
    /// <summary>Shader Storage Buffer Object</summary>
    [DebuggerDisplay("SSBO={Value}, Length={Length}")]
    public readonly struct SSBO : IEquatable<SSBO>
    {
#pragma warning disable 0649    // Disable 'Field is never assigned to, and is always default'
        private readonly int _ssbo;
        private readonly int _length;
#pragma warning restore 0649

        internal int Value => _ssbo;
        internal int Length => _length;

        internal bool IsEmpty => _ssbo == Consts.NULL;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static SSBO Create()
        {
            var ssbo = new SSBO();
            Unsafe.AsRef(ssbo._ssbo) = GL.GenBuffer();
            return ssbo;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Delete(ref SSBO ssbo)
        {
            if(!ssbo.IsEmpty) {
                GL.DeleteBuffer(ssbo._ssbo);
                Unsafe.AsRef(ssbo._ssbo) = Consts.NULL;
                Unsafe.AsRef(ssbo._length) = 0;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Bind(in SSBO ssbo)
        {
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, ssbo._ssbo);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void BindBase(in SSBO ssbo, int index)
        {
            Bind(ssbo);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, index, ssbo._ssbo);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe void LoadNewData<T>(ref SSBO ssbo, ReadOnlySpan<T> data, BufferUsage usage) where T : unmanaged
        {
            if(data.IsEmpty) { return; }
            Bind(ssbo);
            fixed(T* ptr = data) {
                GL.BufferData(BufferTarget.ShaderStorageBuffer, sizeof(T) * data.Length, (IntPtr)ptr, usage.ToBufferUsageHint());
            }
            Unsafe.AsRef(ssbo._length) = data.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe void UpdateSubData<T>(ref SSBO ssbo, int offset, ReadOnlySpan<T> data) where T : unmanaged
        {
            var size = sizeof(T) * data.Length;
            if(ssbo.IsEmpty) { throw new ArgumentException(); }
            if(offset + size > ssbo._length) { throw new ArgumentOutOfRangeException(); }
            Bind(ssbo);
            fixed(T* ptr = data) {
                GL.BufferSubData(BufferTarget.ShaderStorageBuffer, (IntPtr)offset, size, (IntPtr)ptr);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Unbind()
        {
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, Consts.NULL);
        }

        public readonly override string ToString() => _ssbo.ToString();

        public override bool Equals(object? obj) => obj is SSBO ssbo && Equals(ssbo);

        public bool Equals(SSBO other) => _ssbo == other._ssbo && _length == other._length;

        public override int GetHashCode() => HashCode.Combine(_ssbo, _length);

        public static bool operator ==(SSBO left, SSBO right) => left.Equals(right);

        public static bool operator !=(SSBO left, SSBO right) => !(left == right);
    }
}
