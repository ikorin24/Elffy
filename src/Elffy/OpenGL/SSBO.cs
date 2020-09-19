#nullable enable
using Elffy.Core;
using OpenToolkit.Graphics.OpenGL4;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Elffy.OpenGL
{
    /// <summary>Shader Storage Buffer Object</summary>
    [DebuggerDisplay("SSBO={Value}, Length={Length}")]
    public readonly struct SSBO : IEquatable<SSBO>
    {
        private readonly int _ssbo;
        private readonly int _length;

        internal int Value => _ssbo;
        internal int Length => _length;

        public bool IsEmpty => _ssbo == Consts.NULL;

        private SSBO(int ssbo)
        {
            _ssbo = ssbo;
            _length = 0;
        }

        internal static SSBO Create()
        {
            GLAssert.EnsureContext();
            return new SSBO(GL.GenBuffer());
        }

        internal static void Delete(ref SSBO ssbo)
        {
            if(!ssbo.IsEmpty) {
                GLAssert.EnsureContext();
                GL.DeleteBuffer(ssbo._ssbo);
                ssbo = default;
            }
        }

        internal static void Bind(in SSBO ssbo)
        {
            GLAssert.EnsureContext();
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, ssbo._ssbo);
        }

        internal static void BindBase(in SSBO ssbo, int index)
        {
            Bind(ssbo);
            GLAssert.EnsureContext();
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, index, ssbo._ssbo);
        }

        internal static unsafe void LoadNewData<T>(ref SSBO ssbo, ReadOnlySpan<T> data, BufferUsage usage) where T : unmanaged
        {
            if(data.IsEmpty) { return; }
            Bind(ssbo);
            fixed(T* ptr = data) {
                GLAssert.EnsureContext();
                GL.BufferData(BufferTarget.ShaderStorageBuffer, sizeof(T) * data.Length, (IntPtr)ptr, usage.ToBufferUsageHint());
            }
            Unsafe.AsRef(ssbo._length) = data.Length;
        }

        internal static unsafe void UpdateSubData<T>(in SSBO ssbo, int offset, ReadOnlySpan<T> data) where T : unmanaged
        {
            if(ssbo.IsEmpty) { ThrowEmpty(); }
            var size = sizeof(T) * data.Length;
            if(offset + size > ssbo._length) { ThrowOutOfRange(); }
            Bind(ssbo);
            fixed(T* ptr = data) {
                GLAssert.EnsureContext();
                GL.BufferSubData(BufferTarget.ShaderStorageBuffer, (IntPtr)offset, size, (IntPtr)ptr);
            }

            static void ThrowEmpty() => throw new ArgumentException($"{nameof(ssbo)} is empty.");
            static void ThrowOutOfRange() => throw new ArgumentOutOfRangeException();
        }

        internal static void Unbind()
        {
            GLAssert.EnsureContext();
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, Consts.NULL);
        }

        public readonly override string ToString() => _ssbo.ToString();

        public override bool Equals(object? obj) => obj is SSBO ssbo && Equals(ssbo);

        public bool Equals(SSBO other) => (_ssbo == other._ssbo) && (_length == other._length);

        public override int GetHashCode() => HashCode.Combine(_ssbo, _length);
    }
}
