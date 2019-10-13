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
using Elffy.Effective;
using System.Runtime.InteropServices;

namespace Elffy
{
    #region class Texture
    public sealed class Texture : IDisposable
    {
        #region private member
        private const int BYTE_PER_PIXEL = 4;
        private bool _disposed;
        private int _texture;
        #endregion

        private readonly PixelFormat _pixelFormat = PixelFormat.Bgra;

        #region constructor
        internal Texture(int width, int height)
        {
            Dispatcher.ThrowIfNotMainThread();
            try {
                using(var pixels = new UnmanagedArray<byte>(width * height * 4)) {
                    for(int i = 0; i < pixels.Length; i++) {
                        pixels[i] = 0xFF;
                    }
                    _texture = GL.GenTexture();                     // テクスチャ用バッファを確保
                    SetTexture(TextureExpansionMode.Bilinear, TextureExpansionMode.Bilinear, pixels.Ptr, width, height);
                }
            }
            catch(Exception ex) {
                GL.DeleteTexture(_texture);
                throw ex;
            }
        }

        public Texture(string resource)
        {
            Dispatcher.ThrowIfNotMainThread();
            try {
                using(var pixels = LoadFromResource(resource, out var width, out var height)) {
                    _texture = GL.GenTexture();                             // テクスチャ用バッファを確保
                    SetTexture(TextureExpansionMode.Bilinear, TextureExpansionMode.Bilinear, pixels.Ptr, width, height);
                }
            }
            catch(Exception ex) {
                GL.DeleteTexture(_texture);
                throw ex;
            }
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

        #region SwitchBind
        /// <summary>
        /// 現在のOpenGLのTextureをこのインスタンスのテクスチャに切り替えます<para/>
        /// ※ 必要な操作を終えた後、必ず GL.BindTexture(TextureTarget.Texture2D, 0) でバインド解除をしてください。<para/>
        /// </summary>
        internal void SwitchBind()
        {
            GL.BindTexture(TextureTarget.Texture2D, _texture);
        }
        #endregion

        #region ReverseYAxis
        /// <summary>画像のY軸を反転させます</summary>
        /// <param name="ptr">ピクセル配</param>
        /// <param name="width">画像幅</param>
        /// <param name="height">画像高</param>
        /// <param name="pixels">反転させたピクセル配列</param>
        internal static void ReverseYAxis(IntPtr ptr, int width, int height, out UnmanagedArray<byte> pixels)
        {
            pixels = new UnmanagedArray<byte>(width * height * BYTE_PER_PIXEL);
            for(int i = 0; i < height; i++) {
                var row = height - i - 1;
                var head = ptr + width * row * BYTE_PER_PIXEL;
                pixels.CopyFrom(head, i * width * BYTE_PER_PIXEL, width * BYTE_PER_PIXEL);
            }
        }

        /// <summary>画像のY軸を反転させます</summary>
        /// <param name="ptr">ピクセル配</param>
        /// <param name="width">画像幅</param>
        /// <param name="height">画像高</param>
        /// <param name="pixels">反転させたピクセル配列</param>
        internal static void ReverseYAxis(IntPtr ptr, int width, int height, byte[] pixels)
        {
            for(int i = 0; i < height; i++) {
                var row = height - i - 1;
                var head = ptr + width * row * BYTE_PER_PIXEL;
                Marshal.Copy(head, pixels, i * width * BYTE_PER_PIXEL, width * BYTE_PER_PIXEL);
            }
        }
        #endregion

        #region SetTexture
        /// <summary>バッファにTextureを読み込みます</summary>
        /// <param name="minMode">縮小方法</param>
        /// <param name="magMode">拡大方法</param>
        /// <param name="pixels">テクスチャのピクセル配列</param>
        /// <param name="pixelWidth">テクスチャのピクセル幅</param>
        /// <param name="pixelHeight">テクスチャのピクセル高</param>
        private void SetTexture(TextureExpansionMode minMode, TextureExpansionMode magMode, IntPtr pixels, int pixelWidth, int pixelHeight)
        {
            GL.BindTexture(TextureTarget.Texture2D, _texture);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)minMode);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)magMode);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, pixelWidth, pixelHeight, 0, _pixelFormat, PixelType.UnsignedByte, pixels);
            GL.BindTexture(TextureTarget.Texture2D, Consts.NULL);       // バインド解除
        }
        #endregion

        #region LoadFromResource
        private UnmanagedArray<byte> LoadFromResource(string resource, out int pixelWidth, out int pixelHeigh)
        {
            using(var stream = Resources.GetStream(resource))
            using(var bmp = new Bitmap(stream)) {
                pixelWidth = bmp.Width;
                pixelHeigh = bmp.Height;
                var bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), 
                                           System.Drawing.Imaging.ImageLockMode.ReadOnly, 
                                           System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
                ReverseYAxis(bmpData.Scan0, bmp.Width, bmp.Height, out var pixels);
                bmp.UnlockBits(bmpData);
                return pixels;
            }
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
                Dispatcher.Invoke(() => { GL.DeleteTexture(_texture); });
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
        Bilinear = TextureMinFilter.Linear,
        /// <summary>最近傍補間</summary>
        NearestNeighbor = TextureMinFilter.Nearest,
    }
    #endregion enum TextureExpansionMode
}
