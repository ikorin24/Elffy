#nullable enable
using System;
using System.Diagnostics;
using OpenTK.Graphics.OpenGL4;
using Elffy.Core;

namespace Elffy.Graphics.OpenGL
{
    /// <summary>Vertex array object of OpenGL</summary>
    [DebuggerDisplay("VAO={_vao}")]
    public readonly struct VAO : IEquatable<VAO>
    {
        private readonly int _vao;

        internal int Value => _vao;

        /// <summary>Get whether the <see cref="VAO "/> is empty or not.</summary>
        public bool IsEmpty => _vao == Consts.NULL;

        /// <summary>Get empty vertex array object. (that means 0)</summary>
        public static VAO Empty => default;

        private VAO(int vao)
        {
            _vao = vao;
        }

        /// <summary>Create new vertex array object. (Call glGenVertexArray)</summary>
        /// <returns>new <see cref="VAO"/></returns>
        public static VAO Create()
        {
            GLAssert.EnsureContext();
            return new VAO(GL.GenVertexArray());
        }

        /// <summary>Delete <see cref="VAO"/>. (Call glDeleteVertexArray)</summary>
        /// <param name="vao">vertex array object to delete</param>
        public static void Delete(ref VAO vao)
        {
            if(vao._vao != Consts.NULL) {
                GLAssert.EnsureContext();
                GL.DeleteVertexArray(vao._vao);
                vao = default;
            }
        }

        /// <summary>call glBindVertexArray</summary>
        /// <param name="vao">vertex array object to bind</param>
        public static void Bind(in VAO vao)
        {
            GLAssert.EnsureContext();
            GL.BindVertexArray(vao._vao);
        }

        /// <summary>call glBindVertexArray with 0</summary>
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
