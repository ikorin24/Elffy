﻿using OpenTK;
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
        private static readonly int _color4Size = Marshal.SizeOf<Color4>();

        public Vector3 Position;
        public Vector3 Normal;
        public Color4 Color;
        public Vector2 TexCoord;

        public static readonly int Size = Marshal.SizeOf<Vertex>();

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
            GL.TexCoordPointer(4, TexCoordPointerType.Float, Size, Vector3.SizeInBytes * 2 + _color4Size);  // テクスチャ座標
        }
    }
}