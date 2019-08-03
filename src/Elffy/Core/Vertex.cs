using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Elffy.Core
{
#if SUPPORT_VERTEX_COLOR
    [DebuggerDisplay("{Position}")]
    public struct Vertex
    {
        private static readonly int _color4Size = Marshal.SizeOf<Color4>();

        public Vector3 Position;
        public Vector3 Normal;
        public Color4 Color;
        public Vector2 TexCoord;

        public static readonly int Size = Marshal.SizeOf<Vertex>();

        public Vertex(Vector3 position, Vector3 normal, Vector2 texcoord)
        {
            Position = position;
            Normal = normal;
            Color = Color4.Red;
            TexCoord = texcoord;
        }

        public Vertex(Vector3 position, Vector3 normal, Color4 color, Vector2 texcoord)
        {
            Position = position;
            Normal = normal;
            Color = color;
            TexCoord = texcoord;
        }

        internal static void GLSetStructLayout()
        {
            // 構造体のメモリレイアウトをOpenGLに設定する
            GL.VertexPointer(3, VertexPointerType.Float, Size, 0);                          // 頂点の位置
            GL.NormalPointer(NormalPointerType.Float, Size, Vector3.SizeInBytes);           // 頂点の法線
            GL.ColorPointer(4, ColorPointerType.Float, Size, Vector3.SizeInBytes * 2);      // 頂点の色
            GL.TexCoordPointer(2, TexCoordPointerType.Float, Size, Vector3.SizeInBytes * 2 + _color4Size);  // テクスチャ座標
        }
    }
#else
    [DebuggerDisplay("{Position}")]
    public struct Vertex
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector2 TexCoord;

        public static readonly int Size = Marshal.SizeOf<Vertex>();

        public Vertex(Vector3 position, Vector3 normal, Vector2 texcoord)
        {
            Position = position;
            Normal = normal;
            TexCoord = texcoord;
        }

        internal static void GLSetStructLayout()
        {
            // 構造体のメモリレイアウトをOpenGLに設定する
            GL.VertexPointer(3, VertexPointerType.Float, Size, 0);                          // 頂点の位置
            GL.NormalPointer(NormalPointerType.Float, Size, Vector3.SizeInBytes);           // 頂点の法線
            GL.TexCoordPointer(2, TexCoordPointerType.Float, Size, Vector3.SizeInBytes * 2);  // テクスチャ座標
        }
    }
#endif
}
