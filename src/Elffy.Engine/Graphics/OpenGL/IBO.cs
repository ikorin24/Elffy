#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using OpenTK.Graphics.OpenGL4;

namespace Elffy.Graphics.OpenGL
{
    /// <summary>Index buffer object of OpenGL. (Sometimes it is called element buffer object.)</summary>
    [DebuggerDisplay("IBO={_ibo}, Length={_length}")]
    public readonly struct IBO : IEquatable<IBO>
    {
        private readonly int _ibo;
        private readonly uint _length;

        internal int Value => _ibo;

        public uint Length => _length;

        private IBO(int ibo)
        {
            _ibo = ibo;
            _length = 0;
        }

        /// <summary>Create new <see cref="IBO"/> (call glGenBuffer)</summary>
        /// <returns>new index buffer object</returns>
        public static IBO Create()
        {
            GLAssert.EnsureContext();
            return new IBO(GL.GenBuffer());
        }

        /// <summary>Delete <see cref="IBO"/> (call glDeleteBuffer)</summary>
        /// <param name="ibo">index buffer object to delete</param>
        public static void Delete(ref IBO ibo)
        {
            if(ibo._ibo != Consts.NULL) {
                GLAssert.EnsureContext();
                GL.DeleteBuffer(ibo._ibo);
                ibo = default;
            }
        }

        /// <summary>Call glBindBuffer with GL_ELEMENT_ARRAY_BUFFER</summary>
        /// <param name="ibo"></param>
        public static void Bind(in IBO ibo)
        {
            GLAssert.EnsureContext();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ibo._ibo);
        }

        /// <summary>Call glBindBuffer with GL_ELEMENT_ARRAY_BUFFER and 0</summary>
        public static void Unbind()
        {
            GLAssert.EnsureContext();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, Consts.NULL);
        }

        /// <summary>Bind ibo and set buffer data. (Call glBindBuffer and glBufferData)</summary>
        /// <param name="ibo">index buffer object to bind and set data</param>
        /// <param name="indices">indices data to send to ibo</param>
        /// <param name="usage">buffer usage hint</param>
        public static unsafe void BindBufferData(ref IBO ibo, ReadOnlySpan<int> indices, BufferUsageHint usage)
        {
            Bind(ibo);
            fixed(int* ptr = indices) {
                GLAssert.EnsureContext();
                GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(int), (IntPtr)ptr, usage);
            }
            Unsafe.AsRef(ibo._length) = (uint)indices.Length;
        }

        public static unsafe void BindBufferData(ref IBO ibo, int* indices, uint length, BufferUsageHint usage)
        {
            // OpenGL does not support 64bit Length IBO.
            // The max is UInt32.MaxLength

            const uint Max = uint.MaxValue / sizeof(int);
            if(length > Max) {
                ThrowTooLarge();
                static void ThrowTooLarge() => throw new ArgumentOutOfRangeException("Buffer size is too large.");
            }
            uint size = length * sizeof(int);
            Bind(ibo);
            GLAssert.EnsureContext();
            GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)size, (IntPtr)indices, usage);
            Unsafe.AsRef(ibo._length) = length;
        }

        public readonly override string ToString() => _ibo.ToString();

        public readonly override bool Equals(object? obj) => obj is IBO ibo && Equals(ibo);

        public readonly bool Equals(IBO other) => (_ibo == other._ibo) && (_length == other._length);

        public readonly override int GetHashCode() => HashCode.Combine(_ibo, _length);
    }
}
