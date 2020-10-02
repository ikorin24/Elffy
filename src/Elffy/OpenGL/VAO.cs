#nullable enable
using System;
using System.Diagnostics;
using OpenTK.Graphics.OpenGL4;
using Elffy.Core;

namespace Elffy.OpenGL
{
    [DebuggerDisplay("VAO={_vao}")]
    public readonly struct VAO : IEquatable<VAO>
    {
        private readonly int _vao;

        internal int Value => _vao;
        public bool IsEmpty => _vao == Consts.NULL;

        private VAO(int vao)
        {
            _vao = vao;
        }

        internal static VAO Create()
        {
            GLAssert.EnsureContext();
            return new VAO(GL.GenVertexArray());
        }

        internal static void Delete(ref VAO vao)
        {
            if(vao._vao != Consts.NULL) {
                GLAssert.EnsureContext();
                GL.DeleteVertexArray(vao._vao);
                vao = default;
            }
        }

        public static void Bind(in VAO vao)
        {
            GLAssert.EnsureContext();
            GL.BindVertexArray(vao._vao);
        }

        public static void Unbind()
        {
            GLAssert.EnsureContext();
            GL.BindVertexArray(Consts.NULL);
        }



        public readonly override string ToString() => _vao.ToString();

        public readonly override bool Equals(object? obj) => obj is VAO vao && Equals(vao);

        public readonly bool Equals(VAO other) => _vao == other._vao;

        public readonly override int GetHashCode() => _vao.GetHashCode();
    }
}
