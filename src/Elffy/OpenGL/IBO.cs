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

        private readonly int _ibo;

        internal readonly int Value => _ibo;
        internal readonly bool IsEmpty => _ibo == Consts.NULL;

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

        public readonly void BindBufferData(int size, IntPtr ptr, BufferUsageHint usage)
        {
            Bind();
            GL.BufferData(BufferTarget.ElementArrayBuffer, size, ptr, usage);
        }




        public override string ToString() => _ibo.ToString();

        public override bool Equals(object? obj) => obj is IBO ibo && Equals(ibo);

        public bool Equals(IBO other) => _ibo == other._ibo;

        public override int GetHashCode() => HashCode.Combine(_ibo);

        public static bool operator ==(IBO left, IBO right) => left.Equals(right);

        public static bool operator !=(IBO left, IBO right) => !(left == right);
    }
}
