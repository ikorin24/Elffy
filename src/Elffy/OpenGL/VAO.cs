#nullable enable
using System;
using System.Runtime.CompilerServices;
using OpenTK.Graphics.OpenGL;
using Elffy.Core;

namespace Elffy.OpenGL
{
    public readonly struct VAO : IEquatable<VAO>
    {
        // バッファの削除は internal にするために、IDispose.Dispose にしない。interface の実装は public になってしまう。
        // int へのキャストを実装してはいけない。(public になるため)

        private readonly int _vao;

        internal readonly int Value => _vao;
        internal readonly bool IsEmpty => _vao == Consts.NULL;

        public static VAO Empty => new VAO();

        internal static VAO Create()
        {
            var vao = new VAO();
            Unsafe.AsRef(vao._vao) = GL.GenVertexArray();
            return vao;
        }

        internal readonly void Delete()
        {
            if(!IsEmpty) {
                GL.DeleteVertexArray(_vao);
                Unsafe.AsRef(_vao) = Consts.NULL;
            }
        }

        public readonly void Bind()
        {
            GL.BindVertexArray(_vao);
        }

        public readonly void Unbind()
        {
            GL.BindVertexArray(_vao);
        }





        public readonly override string ToString() => _vao.ToString();

        public readonly override bool Equals(object? obj) => obj is VAO vao && Equals(vao);

        public readonly bool Equals(VAO other) => _vao == other._vao;

        public readonly override int GetHashCode() => HashCode.Combine(_vao);

        public static bool operator ==(VAO left, VAO right) => left.Equals(right);

        public static bool operator !=(VAO left, VAO right) => !(left == right);
    }
}
