#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using OpenToolkit.Graphics.OpenGL4;
using Elffy.Core;

namespace Elffy.OpenGL
{
    [DebuggerDisplay("VBO={Value}, Length={Length}, ElementSize={ElementSize}")]
    public readonly struct VBO : IEquatable<VBO>
    {
        // バッファの削除は internal にするために、IDispose.Dispose にしない。interface の実装は public になってしまう。
        // int へのキャストを実装してはいけない。(public になるため)


#pragma warning disable 0649    // Disable 'Field is never assigned to, and is always default'
        private readonly int _vbo;
#pragma warning restore 0649
        internal readonly int Length;

        internal readonly int ElementSize;

        internal readonly int Value => _vbo;
        internal readonly bool IsEmpty => _vbo == Consts.NULL;

        /// <summary>Map vbo to memory as read-write.</summary>
        /// <typeparam name="T">elements type</typeparam>
        /// <returns>returns <see cref="MappedBuffer{T}"/>, which generates <see cref="Span{T}"/>.</returns>
        public readonly MappedBuffer<T> Map<T>() where T : unmanaged
        {
            return new MappedBuffer<T>(this);
        }

        /// <summary>Map vbo to memory as read-only</summary>
        /// <typeparam name="T">elements type</typeparam>
        /// <returns>returns <see cref="ReadOnlyMappedBuffer{T}"/>, which generates <see cref="ReadOnlySpan{T}"/>.</returns>
        public readonly ReadOnlyMappedBuffer<T> ReadOnlyMap<T>() where T : unmanaged
        {
            return new ReadOnlyMappedBuffer<T>(this);
        }

        /// <summary>Create new vertex buffer object</summary>
        /// <returns>new <see cref="VBO"/></returns>
        internal static VBO Create()
        {
            var vbo = new VBO();
            Unsafe.AsRef(vbo._vbo) = GL.GenBuffer();
            return vbo;
        }

        /// <summary>Delete vertex buffer object</summary>
        /// <param name="vbo"><see cref="VBO"/> to delete</param>
        internal static unsafe void Delete(ref VBO vbo)
        {
            if(!vbo.IsEmpty) {
                GL.DeleteBuffer(vbo.Value);
                Unsafe.AsRef(vbo.ElementSize) = default;
                Unsafe.AsRef(vbo) = default;
            }
        }

        /// <summary>Bind vertex buffer object</summary>
        /// <param name="vbo"><see cref="VBO"/> to bind</param>
        public static void Bind(in VBO vbo)
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo._vbo);
        }

        /// <summary>Unbind vertex buffer object</summary>
        public static void Unbind()
        {
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
            Unsafe.AsRef(vbo.ElementSize) = sizeof(T);
            Unsafe.AsRef(vbo.Length) = vertices.Length;
            fixed(T* ptr = vertices) {
                GL.BufferData(BufferTarget.ArrayBuffer, vbo.Length * vbo.ElementSize, (IntPtr)ptr, usage);
            }
        }



        public override string ToString() => _vbo.ToString();

        public override bool Equals(object? obj) => obj is VBO vbo && Equals(vbo);

        public bool Equals(VBO other) => (_vbo == other._vbo) && (Length == other.Length) && (ElementSize == other.ElementSize);

        public override int GetHashCode() => HashCode.Combine(_vbo, Length, ElementSize);

        public static bool operator ==(in VBO left, in VBO right) => left.Equals(right);

        public static bool operator !=(in VBO left, in VBO right) => !(left == right);
    }
}
