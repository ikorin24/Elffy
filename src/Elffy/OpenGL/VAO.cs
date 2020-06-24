#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using OpenToolkit.Graphics.OpenGL;
using Elffy.Core;

namespace Elffy.OpenGL
{
    [DebuggerDisplay("VAO={Value}")]
    public readonly struct VAO : IEquatable<VAO>
    {
        // バッファの削除は internal にするために、IDispose.Dispose にしない。interface の実装は public になってしまう。
        // int へのキャストを実装してはいけない。(public になるため)


#pragma warning disable 0649    // Disable 'Field is never assigned to, and is always default'
        private readonly int _vao;
#pragma warning restore 0649

        internal readonly int Value => _vao;
        internal readonly bool IsEmpty => _vao == Consts.NULL;

        public static VAO Empty => new VAO();

        internal static VAO Create()
        {
            var vao = new VAO();
            Unsafe.AsRef(vao._vao) = GL.GenVertexArray();
            return vao;
        }

        internal static void Delete(ref VAO vao)
        {
            if(!vao.IsEmpty) {
                GL.DeleteVertexArray(vao._vao);
                Unsafe.AsRef(vao) = default;
            }
        }

        public static void Bind(in VAO vao)
        {
            GL.BindVertexArray(vao._vao);
        }

        public static void Unbind()
        {
            GL.BindVertexArray(Consts.NULL);
        }



        public readonly override string ToString() => _vao.ToString();

        public readonly override bool Equals(object? obj) => obj is VAO vao && Equals(vao);

        public readonly bool Equals(VAO other) => _vao == other._vao;

        public readonly override int GetHashCode() => HashCode.Combine(_vao);

        public static bool operator ==(VAO left, VAO right) => left.Equals(right);

        public static bool operator !=(VAO left, VAO right) => !(left == right);
    }
}
