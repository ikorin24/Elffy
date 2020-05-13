#nullable enable
using System;
using System.Runtime.CompilerServices;
using OpenTK.Graphics.OpenGL;
using Elffy.Core;

namespace Elffy.OpenGL
{
    public readonly struct VBO : IEquatable<VBO>
    {
        // バッファの削除は internal にするために、IDispose.Dispose にしない。interface の実装は public になってしまう。
        // int へのキャストを実装してはいけない。(public になるため)

        private readonly int _vbo;
        
        internal readonly int Value => _vbo;
        internal readonly bool IsEmpty => _vbo == Consts.NULL;

        public static VBO Empty => new VBO();

        internal static VBO Create()
        {
            var vbo = new VBO();
            Unsafe.AsRef(vbo._vbo) = GL.GenBuffer();
            return vbo;
        }

        internal readonly void Delete()
        {
            if(!IsEmpty) {
                GL.DeleteBuffer(_vbo);
                Unsafe.AsRef(_vbo) = Consts.NULL;
            }
        }

        public readonly void Bind()
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        }




        public override string ToString() => _vbo.ToString();

        public override bool Equals(object? obj) => obj is VBO vbo && Equals(vbo);

        public bool Equals(VBO other) => _vbo == other._vbo;

        public override int GetHashCode() => HashCode.Combine(_vbo);

        public static bool operator ==(VBO left, VBO right) => left.Equals(right);

        public static bool operator !=(VBO left, VBO right) => !(left == right);
    }
}
