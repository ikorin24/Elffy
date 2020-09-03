#nullable enable
using Elffy.OpenGL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using OpenToolkit.Graphics.OpenGL4;
using Elffy.AssemblyServices;

namespace Elffy.Core
{
    public class PostProcessor
    {
        private FBO _fbo;
        private TextureObject _to;
        private RBO _rbo;

        internal PostProcessor()
        {
        }

        internal void UpdateSize(int width, int height)
        {
            if(width <= 0 || height <= 0) { return; }

            CreateFBO(width, height, out var fbo, out var to, out var rbo);

            FBO.Delete(ref _fbo);
            TextureObject.Delete(ref _to);
            RBO.Delete(ref _rbo);

            _fbo = fbo;
            _to = to;
            _rbo = rbo;
        }

        private static void CreateFBO(int width, int height, out FBO fbo, out TextureObject to, out RBO rbo)
        {
            Debug.Assert(width > 0);
            Debug.Assert(height > 0);

            fbo = FBO.Create();
            {
                FBO.Bind(fbo);
                to = TextureObject.Create();
                {
                    TextureObject.Bind2D(to, TextureUnitNumber.Unit0);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                    GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, width, height, 0, PixelFormat.Rgb, PixelType.UnsignedByte, IntPtr.Zero);
                    TextureObject.Unbind2D(TextureUnitNumber.Unit0);
                }
                FBO.SetTexture2DBuffer(to);
                rbo = RBO.Create();
                {
                    RBO.Bind(rbo);
                    RBO.SetStorage(width, height);
                    RBO.Unbind();
                }
                FBO.SetRenderBuffer(rbo);
                FBO.Unbind(fbo);
            }

            if(AssemblyState.IsDebug && !FBO.CheckStatus(out var status)) {
                throw new Exception(status.ToString());
            }
        }
    }
}
