#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using OpenTK.Graphics.OpenGL4;
using Elffy.Core;

namespace Elffy.OpenGL
{
    /// <summary>Vetex buffer object of OpenGL</summary>
    [DebuggerDisplay("VBO={_vbo}, Length={_length}, ElementSize={_elementSize}")]
    public readonly struct VBO : IEquatable<VBO>
    {
        private readonly int _vbo;
        private readonly int _length;
        private readonly int _elementSize;

        internal int Value => _vbo;
        internal int Length => _length;
        internal int ElementSize => _elementSize;

        /// <summary>Get whether the vertex buffer object is empty or not.</summary>
        public bool IsEmpty => _vbo == Consts.NULL;

        private VBO(int vbo)
        {
            _vbo = vbo;
            _length = 0;
            _elementSize = 0;
        }

        /// <summary>Create new vertex buffer object</summary>
        /// <returns>new <see cref="VBO"/></returns>
        public static VBO Create()
        {
            GLAssert.EnsureContext();
            return new VBO(GL.GenBuffer());
        }

        /// <summary>Delete vertex buffer object</summary>
        /// <param name="vbo"><see cref="VBO"/> to delete</param>
        public static unsafe void Delete(ref VBO vbo)
        {
            if(vbo._vbo != Consts.NULL) {
                GLAssert.EnsureContext();
                GL.DeleteBuffer(vbo._vbo);
                vbo = default;
            }
        }

        /// <summary>Bind vertex buffer object</summary>
        /// <param name="vbo"><see cref="VBO"/> to bind</param>
        public static void Bind(in VBO vbo)
        {
            GLAssert.EnsureContext();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo._vbo);
        }

        /// <summary>Unbind vertex buffer object</summary>
        public static void Unbind()
        {
            GLAssert.EnsureContext();
            GL.BindBuffer(BufferTarget.ArrayBuffer, Consts.NULL);
        }

        /// <summary>Bind <see cref="VBO"/> and send data</summary>
        /// <typeparam name="T">data type</typeparam>
        /// <param name="vbo"><see cref="VBO"/> to send data to</param>
        /// <param name="vertices">sended data to vbo</param>
        /// <param name="usage">buffer usage hint</param>
        internal static unsafe void BindBufferData<T>(ref VBO vbo, ReadOnlySpan<T> vertices, BufferUsageHint usage) where T : unmanaged
        {
            Bind(vbo);
            Unsafe.AsRef(vbo._elementSize) = sizeof(T);
            Unsafe.AsRef(vbo._length) = vertices.Length;
            fixed(T* ptr = vertices) {
                GLAssert.EnsureContext();
                GL.BufferData(BufferTarget.ArrayBuffer, vbo._length * vbo._elementSize, (IntPtr)ptr, usage);
            }
        }



        public override string ToString() => _vbo.ToString();

        public override bool Equals(object? obj) => obj is VBO vbo && Equals(vbo);

        public bool Equals(VBO other) => (_vbo == other._vbo) && (_length == other._length) && (_elementSize == other._elementSize);

        public override int GetHashCode() => HashCode.Combine(_vbo, _length, _elementSize);
    }
}
