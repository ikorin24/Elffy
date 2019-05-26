using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elffy
{
    public class Texture : IDisposable
    {
        private int _texture;

        public Texture()
        {
            _texture = GL.GenTexture();

            //テクスチャ用バッファのひもづけ
            GL.BindTexture(TextureTarget.Texture2D, _texture);

            //テクスチャの設定
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

            //テクスチャの色情報を作成
            int size = 8;
            float[,,] colors = new float[size, size, 4];
            for(int i = 0; i < colors.GetLength(0); i++) {
                for(int j = 0; j < colors.GetLength(1); j++) {
                    colors[i, j, 0] = (float)i / size;
                    colors[i, j, 1] = (float)j / size;
                    colors[i, j, 2] = 0.0f;
                    colors[i, j, 3] = 1.0f;
                }
            }

            //テクスチャ用バッファに色情報を流し込む
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, size, size, 0, PixelFormat.Rgba, PixelType.Float, colors);
        }

        public void Dispose()
        {
            GL.DeleteTexture(_texture);
        }

        internal void Apply()
        {
            //テクスチャ用バッファに色情報を流し込む
            //GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, size, size, 0, PixelFormat.Rgba, PixelType.Float, colors);
        }
    }
}
