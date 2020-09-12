#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using OpenToolkit.Graphics.OpenGL4;
using Elffy.Core;

namespace Elffy.OpenGL
{
    [DebuggerDisplay("IBO={_ibo}, Length={_length}")]
    public readonly struct IBO : IEquatable<IBO>
    {
        private readonly int _ibo;
        private readonly int _length;

        internal int Value => _ibo;

        internal int Length => _length;

        private IBO(int ibo)
        {
            _ibo = ibo;
            _length = 0;
        }

        internal static IBO Create()
        {
            GLAssert.EnsureContext();
            return new IBO(GL.GenBuffer());
        }

        internal static void Delete(ref IBO ibo)
        {
            if(ibo._ibo != Consts.NULL) {
                GLAssert.EnsureContext();
                GL.DeleteBuffer(ibo._ibo);
                ibo = default;
            }
        }

        internal static void Bind(in IBO ibo)
        {
            GLAssert.EnsureContext();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ibo._ibo);
        }

        internal static void Unbind()
        {
            GLAssert.EnsureContext();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, Consts.NULL);
        }

        internal static unsafe void BindBufferData(ref IBO ibo, ReadOnlySpan<int> indices, BufferUsageHint usage)
        {
            Bind(ibo);
            fixed(int* ptr = indices) {
                GLAssert.EnsureContext();
                GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(int), (IntPtr)ptr, usage);
            }
            Unsafe.AsRef(ibo._length) = indices.Length;
        }


        public readonly override string ToString() => _ibo.ToString();

        public readonly override bool Equals(object? obj) => obj is IBO ibo && Equals(ibo);

        public readonly bool Equals(IBO other) => (_ibo == other._ibo) && (_length == other._length);

        public readonly override int GetHashCode() => HashCode.Combine(_ibo, _length);
    }
}
