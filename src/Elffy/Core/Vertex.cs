#nullable enable
using OpenTK.Graphics.OpenGL;
using System;
using System.Diagnostics;

namespace Elffy.Core
{
    [DebuggerDisplay("{Position}")]
    public struct Vertex : IEquatable<Vertex>
    {
        private unsafe static readonly int _color4Size = sizeof(Color4);

        public Vector3 Position;
        public Vector3 Normal;
        public Color4 Color;
        public Vector2 TexCoord;

        public static unsafe readonly int Size = sizeof(Vertex);

        public Vertex(Vector3 position, Vector3 normal, Vector2 texcoord)
        {
            Position = position;
            Normal = normal;
            Color = new Color4(Math.Abs(normal.X), Math.Abs(normal.Y), Math.Abs(normal.Z), 1f);
            TexCoord = texcoord;
        }

        public Vertex(Vector3 position, Vector3 normal, Color4 color, Vector2 texcoord)
        {
            Position = position;
            Normal = normal;
            Color = color;
            TexCoord = texcoord;
        }

        //[Obsolete]
        //internal unsafe static void GLSetStructLayout()
        //{
        //    // 構造体のメモリレイアウトをOpenGLに設定する
        //    GL.VertexPointer(3, VertexPointerType.Float, Size, 0);                          // 頂点の位置
        //    GL.NormalPointer(NormalPointerType.Float, Size, Vector3.SizeInBytes);           // 頂点の法線
        //    GL.ColorPointer(4, ColorPointerType.Float, Size, Vector3.SizeInBytes * 2);      // 頂点の色
        //    GL.TexCoordPointer(2, TexCoordPointerType.Float, Size, Vector3.SizeInBytes * 2 + sizeof(Color4));  // テクスチャ座標
        //}

        internal unsafe static void DefineLayout()
        {
            GL.EnableVertexAttribArray(0);
            GL.EnableVertexAttribArray(1);
            GL.EnableVertexAttribArray(2);
            GL.EnableVertexAttribArray(3);

            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, Size, 0);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, Size, sizeof(Vector3));
            GL.VertexAttribPointer(2, 4, VertexAttribPointerType.Float, false, Size, sizeof(Vector3) + sizeof(Vector3));
            GL.VertexAttribPointer(3, 2, VertexAttribPointerType.Float, false, Size, sizeof(Vector3) + sizeof(Vector3) + sizeof(Color4));
        }

        public override bool Equals(object? obj) => obj is Vertex vertex && Equals(vertex);

        public bool Equals(Vertex other) => Position.Equals(other.Position) &&
                                            Normal.Equals(other.Normal) &&
                                            Color.Equals(other.Color) &&
                                            TexCoord.Equals(other.TexCoord);

        public override int GetHashCode() => HashCode.Combine(Position, Normal, Color, TexCoord);

        public static bool operator ==(Vertex left, Vertex right) => left.Equals(right);

        public static bool operator !=(Vertex left, Vertex right) => !(left == right);
    }
}
