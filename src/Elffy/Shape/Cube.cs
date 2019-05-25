using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using Elffy.Core;

namespace Elffy.Shape
{
    public class Cube : Renderable
    {
        //private int _texture;

        public Cube()
        {
            //const int TEX_WIDTH = 1;
            //const int TEX_HEIGHT = 1;
            //_texture = GL.GenTexture();
            //GL.BindTexture(TextureTarget.Texture2D, _texture);
            //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            //GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, TEX_WIDTH, TEX_HEIGHT, 0, PixelFormat.Rgba, PixelType.UnsignedByte, CreateDefaultTexture());
        }

        protected override void TextureVertex()
        {
            //GL.BindTexture(TextureTarget.Texture2D, _texture);
            DrawCube();
        }

        private void DrawCube()
        {
            GL.Begin(PrimitiveType.Quads);

            GL.Normal3(1.0f, 0.0f, 0.0f);
            GL.Vertex3(1.0f, 1.0f, 1.0f);
            GL.Vertex3(1.0f, -1.0f, 1.0f);
            GL.Vertex3(1.0f, -1.0f, -1.0f);
            GL.Vertex3(1.0f, 1.0f, -1.0f);

            GL.Normal3(-1.0f, 0.0f, 0.0f);
            GL.Vertex3(-1.0f, 1.0f, 1.0f);
            GL.Vertex3(-1.0f, 1.0f, -1.0f);
            GL.Vertex3(-1.0f, -1.0f, -1.0f);
            GL.Vertex3(-1.0f, -1.0f, 1.0f);

            GL.Normal3(0.0f, 1.0f, 0.0f);
            GL.Vertex3(1.0f, 1.0f, 1.0f);
            GL.Vertex3(1.0f, 1.0f, -1.0f);
            GL.Vertex3(-1.0f, 1.0f, -1.0f);
            GL.Vertex3(-1.0f, 1.0f, 1.0f);

            GL.Normal3(0.0f, -1.0f, 0.0f);
            GL.Vertex3(1.0f, -1.0f, 1.0f);
            GL.Vertex3(-1.0f, -1.0f, 1.0f);
            GL.Vertex3(-1.0f, -1.0f, -1.0f);
            GL.Vertex3(1.0f, -1.0f, -1.0f);

            GL.Normal3(0.0f, 0.0f, 1.0f);
            GL.Vertex3(1.0f, 1.0f, 1.0f);
            GL.Vertex3(-1.0f, 1.0f, 1.0f);
            GL.Vertex3(-1.0f, -1.0f, 1.0f);
            GL.Vertex3(1.0f, -1.0f, 1.0f);

            GL.Normal3(0.0f, 0.0f, -1.0f);
            GL.Vertex3(1.0f, 1.0f, -1.0f);
            GL.Vertex3(1.0f, -1.0f, -1.0f);
            GL.Vertex3(-1.0f, -1.0f, -1.0f);
            GL.Vertex3(-1.0f, 1.0f, -1.0f);

            GL.End();
        }
    }
}
