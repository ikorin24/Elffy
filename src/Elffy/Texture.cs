using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elffy.Core;

namespace Elffy
{
    #region class Texture
    public sealed class Texture : IDisposable
    {
        #region private member
        private bool _disposed;
        private int _texture;
        private static readonly float[] EMPTY_TEXTURE = new float[4] { 1f, 1f, 1f, 1f };
        private static readonly int EMPTY_TEXTURE_WIDTH = 1;
        private static readonly int EMPTY_TEXTURE_HEIGHT = 1;
        #endregion

        /// <summary>テクスチャの拡大・縮小方法</summary>
        public TextureExpansionMode ExpansionMode { get; private set; } = TextureExpansionMode.Bilinear;    // public set できるようにすべき？

        #region constructor
        public Texture()
        {
            _texture = GL.GenTexture();                     // テクスチャ用バッファを確保
            SetTexture(TextureExpansionMode.Bilinear, EMPTY_TEXTURE, EMPTY_TEXTURE_WIDTH, EMPTY_TEXTURE_HEIGHT);
        }

        public Texture(string resource, TextureExpansionMode expansionMode)
        {
            var pixels = LoadFromResource(resource, out var width, out var height);
            _texture = GL.GenTexture();                             // テクスチャ用バッファを確保
            SetTexture(expansionMode, pixels, width, height);
        }
        #endregion

        #region SwitchToThis
        /// <summary>OpenGLのCurrentTextureをこのインスタンスのテクスチャに切り替えます</summary>
        internal void SwitchToThis()
        {
            GL.BindTexture(TextureTarget.Texture2D, _texture);
        }
        #endregion

        #region SetTexture
        /// <summary>バッファにTextureを読み込みます</summary>
        /// <param name="expansionMode">拡大縮小方法</param>
        /// <param name="pixels">テクスチャのピクセル配列</param>
        /// <param name="pixelWidth">テクスチャのピクセル幅</param>
        /// <param name="pixelHeight">テクスチャのピクセル高</param>
        private void SetTexture(TextureExpansionMode expansionMode, float[] pixels, int pixelWidth, int pixelHeight)
        {
            GL.BindTexture(TextureTarget.Texture2D, _texture);
            var param = TexExpansionModeToParam(expansionMode);         // テクスチャ拡大縮小の方法
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, param);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, param);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, pixelWidth, pixelHeight, 0, PixelFormat.Rgba, PixelType.Float, pixels);
            GL.BindTexture(TextureTarget.Texture2D, Consts.NULL);       // バインド解除
        }
        #endregion

        #region TexExpansionModeToParam
        private int TexExpansionModeToParam(TextureExpansionMode mode)
        {
            switch(mode) {
                case TextureExpansionMode.Bilinear: return (int)TextureMinFilter.Linear;
                case TextureExpansionMode.NearestNeighbor: return (int)TextureMinFilter.Nearest;
                default: throw new NotSupportedException();
            }
        }
        #endregion

        #region LoadFromResource
        private float[] LoadFromResource(string resource, out int pixelWidth, out int pixelHeigh)
        {
            throw new NotImplementedException();        // TODO: リソースからの画像読み取り
        }
        #endregion

        #region Dispose
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if(!_disposed) {
                if(disposing) {
                    // Release managed resource here.
                }
                GL.DeleteTexture(_texture);
                _disposed = true;
            }
        }
        #endregion
    }
    #endregion class Texture

    #region enum TextureExpansionMode
    /// <summary>テクスチャ拡大縮小方法</summary>
    public enum TextureExpansionMode
    {
        /// <summary>線形補間</summary>
        Bilinear,
        /// <summary>最近傍補間</summary>
        NearestNeighbor,
    }
    #endregion enum TextureExpansionMode
}
