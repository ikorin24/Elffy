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
        private int _texture;

        public Cube()
        {
            const int TEX_WIDTH = 1;
            const int TEX_HEIGHT = 1;
            _texture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, _texture);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, TEX_WIDTH, TEX_HEIGHT, 0, PixelFormat.Rgba, PixelType.UnsignedByte, CreateDefaultTexture());
        }

        protected override void TextureVertex()
        {
            // TODO:
            // 法線方向とか？？

            GL.BindTexture(TextureTarget.Texture2D, _texture);

            const float a = 1f / 3;
            const float b = 2f / 3;
            const float c = 1f / 4;

            GL.Begin(PrimitiveType.Quads);
            GL.TexCoord2(c + 0.0f, 1.0f); GL.Vertex3(-1f, -1f, 1f);
            GL.TexCoord2(c + 1.0f, 1.0f); GL.Vertex3(1f, -1f, 1f);
            GL.TexCoord2(c + 1.0f, 0.0f); GL.Vertex3(1f, 1f, 1f);
            GL.TexCoord2(c + 0.0f, 0.0f); GL.Vertex3(-1f, 1f, 1f);
            GL.End();

            GL.Begin(PrimitiveType.Quads);
            GL.TexCoord2(0.0f, a + 1.0f); GL.Vertex3(-1f, 1f, -1f);
            GL.TexCoord2(1.0f, a + 1.0f); GL.Vertex3(-1f, -1f, -1f);
            GL.TexCoord2(1.0f, a + 0.0f); GL.Vertex3(-1f, -1f, 1f);
            GL.TexCoord2(0.0f, a + 0.0f); GL.Vertex3(-1f, 1f, 1f);
            GL.End();

            GL.Begin(PrimitiveType.Quads);
            GL.TexCoord2(c + 0.0f, a + 1.0f); GL.Vertex3(-1f, -1f, -1f);
            GL.TexCoord2(c + 1.0f, a + 1.0f); GL.Vertex3(1f, -1f, -1f);
            GL.TexCoord2(c + 1.0f, a + 0.0f); GL.Vertex3(1f, -1f, 1f);
            GL.TexCoord2(c + 0.0f, a + 0.0f); GL.Vertex3(-1f, -1f, 1f);
            GL.End();

            GL.Begin(PrimitiveType.Quads);
            GL.TexCoord2(c * 2 + 0.0f, a + 1.0f); GL.Vertex3(1f, -1f, -1f);
            GL.TexCoord2(c * 2 + 1.0f, a + 1.0f); GL.Vertex3(1f, 1f, -1f);
            GL.TexCoord2(c * 2 + 1.0f, a + 0.0f); GL.Vertex3(1f, 1f, 1f);
            GL.TexCoord2(c * 2 + 0.0f, a + 0.0f); GL.Vertex3(1f, -1f, 1f);
            GL.End();

            GL.Begin(PrimitiveType.Quads);
            GL.TexCoord2(c * 3 + 0.0f, a + 1.0f); GL.Vertex3(1f, 1f, -1f);
            GL.TexCoord2(c * 3 + 1.0f, a + 1.0f); GL.Vertex3(-1f, 1f, -1f);
            GL.TexCoord2(c * 3 + 1.0f, a + 0.0f); GL.Vertex3(-1f, 1f, 1f);
            GL.TexCoord2(c * 3 + 0.0f, a + 0.0f); GL.Vertex3(1f, 1f, 1f);
            GL.End();

            GL.Begin(PrimitiveType.Quads);
            GL.TexCoord2(c + 0.0f, b + 1.0f); GL.Vertex3(-1f, 1f, -1f);
            GL.TexCoord2(c + 1.0f, b + 1.0f); GL.Vertex3(1f, 1f, -1f);
            GL.TexCoord2(c + 1.0f, b + 0.0f); GL.Vertex3(1f, -1f, -1f);
            GL.TexCoord2(c + 0.0f, b + 0.0f); GL.Vertex3(-1f, -1f, -1f);
            GL.End();
        }

        private uint[] CreateDefaultTexture()
        {
            // バイト列はABGRの順
            var data = new uint[12];

            //data[0] = 0xFF00FF00;       // Green
            //data[1] = 0xFF00FF00;       // Green
            //data[2] = 0xFF00FF00;       // Green
            //data[3] = 0xFF00FF00;       // Green
            //data[4] = 0xFF00FF00;       // Green
            //data[5] = 0xFF00FF00;       // Green


            //data[1] = 0xFF000000;   // 白 前
            //data[4] = 0xFFFF0000;   // 青 左
            //data[5] = 0xFF00FF00;   // 緑 下
            //data[6] = 0xFF0000FF;   // 赤 右
            //data[7] = 0xFF00FFFF;   // 黄 上
            //data[9] = 0xFFFFFF00;   // 水 奥

            for(int i = 0; i < data.Length; i++) {
                // バイト列はABGRの順
                //data[i] = 0xFFFFFFFF;
                //data[i] = 0xFFFF0000;       // Blue
                data[i] = 0xFF00FF00;       // Green
                //data[i] = 0xFF0000FF;       // Red
                //data[i] = 0x4300FFFF;       // 半透明
            }
            return data;
        }
    }
}
