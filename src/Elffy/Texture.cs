using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elffy.Core;
using System.Drawing;
using Elffy.Threading;

namespace Elffy
{
    #region class Texture
    public sealed class Texture : IDisposable
    {
        #region private member
        private bool _disposed;
        private int _texture;
        private static readonly byte[] EMPTY_TEXTURE = new byte[4] { 0xFF, 0xFF, 0xFF, 0xFF };
        private static readonly int EMPTY_TEXTURE_WIDTH = 1;
        private static readonly int EMPTY_TEXTURE_HEIGHT = 1;
        #endregion

        /// <summary>テクスチャの拡大・縮小方法</summary>
        public TextureExpansionMode ExpansionMode { get; private set; } = TextureExpansionMode.Bilinear;    // public set できるようにすべき？

        private readonly PixelFormat _pixelFormat = PixelFormat.Bgra;

        #region constructor
        /// <summary>真っ白のテクスチャ (size: 1x1) を作成します</summary>
        internal Texture()
        {
            _texture = GL.GenTexture();                     // テクスチャ用バッファを確保
            SetTexture(TextureExpansionMode.Bilinear, EMPTY_TEXTURE, EMPTY_TEXTURE_WIDTH, EMPTY_TEXTURE_HEIGHT);
        }

        internal Texture(int width, int height)
        {
            _texture = GL.GenTexture();                     // テクスチャ用バッファを確保
            var pixels = new byte[width * height * 4];      // TODO: Unmanagedメモリを使って、GCに負荷をかけないように
            for(int i = 0; i < pixels.Length; i++) {
                pixels[i] = 0xFF;
            }
            SetTexture(TextureExpansionMode.Bilinear, pixels, width, height);
        }

        public Texture(string resource, TextureExpansionMode expansionMode)
        {
            var pixels = LoadFromResource(resource, out var width, out var height);
            _texture = GL.GenTexture();                             // テクスチャ用バッファを確保
            SetTexture(expansionMode, pixels, width, height);
        }

        public Texture(string file)     // TODO: 消す テスト用
        {
            byte[] GetPixels(IntPtr ptr, int width, int height)
            {
                const int BYTE_PAR_PIXEL = 4;
                var buf = new byte[width * height * BYTE_PAR_PIXEL];
                for(int i = 0; i < height; i++) {
                    var row = height - i - 1;
                    var head = ptr + width * row * BYTE_PAR_PIXEL;
                    System.Runtime.InteropServices.Marshal.Copy(head, buf, i * width * BYTE_PAR_PIXEL, width * BYTE_PAR_PIXEL);
                }
                return buf;
            }

            var bmp = new Bitmap(file);
            var bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            var pixels = GetPixels(bmpData.Scan0, bmp.Width, bmp.Height);
            bmp.UnlockBits(bmpData);
            SetTexture(TextureExpansionMode.Bilinear, pixels, bmp.Width, bmp.Height);
        }
        #endregion

        /// <summary>テクスチャの一部を更新します</summary>
        /// <param name="newPixels">新しいピクセル配列 (テクスチャ全体のピクセル配列)</param>
        /// <param name="dirtyRegion">変更部分</param>
        internal void UpdateTexture(byte[] newPixels, Rectangle dirtyRegion)
        {
            GL.BindTexture(TextureTarget.Texture2D, _texture);
            GL.TexSubImage2D(TextureTarget.Texture2D, 0, dirtyRegion.X, dirtyRegion.Y, dirtyRegion.Width, dirtyRegion.Height, _pixelFormat, PixelType.UnsignedByte, newPixels);
            GL.BindTexture(TextureTarget.Texture2D, Consts.NULL);
        }

        public void ChangeSize(int width, int height, byte[] pixels)
        {
            GL.BindTexture(TextureTarget.Texture2D, Consts.NULL);
            GL.DeleteTexture(_texture);                             // テクスチャバッファを削除
            SetTexture(ExpansionMode, pixels, width, height);       // 新たにテクスチャバッファを再取得
        }

        #region SwitchToThis
        /// <summary>
        /// 現在のOpenGLのTextureをこのインスタンスのテクスチャに切り替えます<para/>
        /// ※ 必要な操作を終えた後、必ず GL.BindTexture(TextureTarget.Texture2D, 0) でバインド解除をしてください。<para/>
        /// </summary>
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
        private void SetTexture(TextureExpansionMode expansionMode, byte[] pixels, int pixelWidth, int pixelHeight)
        {
            GL.BindTexture(TextureTarget.Texture2D, _texture);
            var param = TexExpansionModeToParam(expansionMode);         // テクスチャ拡大縮小の方法
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, param);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, param);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, pixelWidth, pixelHeight, 0, _pixelFormat, PixelType.UnsignedByte, pixels);
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
        private byte[] LoadFromResource(string resource, out int pixelWidth, out int pixelHeigh)
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

                // OpenGLのバッファの削除はメインスレッドで行う必要がある
                var texture = _texture;
                GameThread.Invoke(() => { GL.DeleteTexture(_texture); });
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
