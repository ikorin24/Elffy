#nullable enable
using System;
using System.Runtime.CompilerServices;
using OpenTK.Graphics.OpenGL;
using Elffy.Core;

namespace Elffy.OpenGL
{
    public readonly struct IBO : IEquatable<IBO>
    {
        // バッファの削除は internal にするために、IDispose.Dispose にしない。interface の実装は public になってしまう。
        // int へのキャストを実装してはいけない。(public になるため)


#pragma warning disable 0649    // Disable 'Field is never assigned to, and is always default'
        private readonly int _ibo;
        private readonly int _length;
#pragma warning restore 0649

        internal readonly int Value => _ibo;
        internal readonly bool IsEmpty => _ibo == Consts.NULL;

        public readonly int Length => _length;

        public static IBO Empty => new IBO();

        internal static IBO Create()
        {
            var ibo = new IBO();
            Unsafe.AsRef(ibo._ibo) = GL.GenBuffer();
            return ibo;
        }

        internal readonly void Delete()
        {
            if(!IsEmpty) {
                GL.DeleteBuffer(_ibo);
                Unsafe.AsRef(_ibo) = Consts.NULL;
                Unsafe.AsRef(_length) = default;
            }
        }

        public readonly void Bind()
        {
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ibo);
        }

        public readonly void Unbind()
        {
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, Consts.NULL);
        }

        public readonly unsafe void BindBufferData(ReadOnlySpan<int> indices, BufferUsageHint usage)
        {
            Bind();
            Unsafe.AsRef(_length) = indices.Length;
            fixed(int* ptr = indices) {
                GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(int), (IntPtr)ptr, usage);
            }
        }




        public readonly override string ToString() => _ibo.ToString();

        public readonly override bool Equals(object? obj) => obj is IBO ibo && Equals(ibo);

        public readonly bool Equals(IBO other) => _ibo == other._ibo;

        public readonly override int GetHashCode() => HashCode.Combine(_ibo);

        public static bool operator ==(IBO left, IBO right) => left.Equals(right);

        public static bool operator !=(IBO left, IBO right) => !(left == right);
    }
}
