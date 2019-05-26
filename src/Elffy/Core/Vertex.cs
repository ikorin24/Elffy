using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Elffy.Core
{
    public struct Vertex
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Color4 Color;

        public static readonly int Size = Marshal.SizeOf<Vertex>();

        public Vertex(Vector3 position, Vector3 normal, Color4 color)
        {
            Position = position;
            Normal = normal;
            Color = color;
        }

        internal static void GLSetPointer()
        {
            GL.VertexPointer(3, VertexPointerType.Float, Size, 0);                      // 頂点の位置情報の場所を指定
            GL.NormalPointer(NormalPointerType.Float, Size, Vector3.SizeInBytes);       // 頂点の法線情報の場所を指定
            GL.ColorPointer(4, ColorPointerType.Float, Size, Vector3.SizeInBytes * 2);  // 頂点の色情報の場所を指定
        }
    }
}
