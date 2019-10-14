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
        private const PixelFormat PIXEL_FORMAT = PixelFormat.Bgra;
        private bool _disposed;
        private int _texture;
        #endregion

        /// <summary>テクスチャの縮小モードを取得します</summary>
        public TextureShrinkMode ShrinkMode { get; private set; }
        /// <summary>テクスチャの拡大モードを取得します</summary>
        public TextureExpansionMode ExpansionMode { get; private set; }
        /// <summary>テクスチャのミップマップモードを取得します (ミップマップ不使用の場合は null)</summary>
        public TextureMipmapMode? MipmapMode { get; private set; }
        /// <summary>テクスチャがミップマップを持っているかどうかを取得します</summary>
        public bool HasMipmap => MipmapMode != null;
        /// <summary>テクスチャのピクセル幅を取得します</summary>
        public int PixelWidth { get; private set; }
        /// <summary>テクスチャのピクセル高さを取得します</summary>
        public int PixelHeight { get; private set; }

        #region constructor
        internal Texture(int width, int height)
        {
            if(width <= 0) { throw new ArgumentOutOfRangeException(nameof(width)); }
            if(height <= 0) { throw new ArgumentOutOfRangeException(nameof(height)); }
            Dispatcher.ThrowIfNotMainThread();
            try {
                using(var pixels = new UnmanagedArray<byte>(width * height * 4)) {
                    for(int i = 0; i < pixels.Length; i++) {
                        pixels[i] = 0xFF;
                    }
                    _texture = GL.GenTexture();
                    SetTexture(TextureShrinkMode.Bilinear, TextureExpansionMode.Bilinear, pixels.Ptr, width, height);
                }
            }
            catch(Exception ex) {
                GL.DeleteTexture(_texture);
                throw ex;
            }
        }

        /// <summary>リソース名を指定してミップマップありのテクスチャを生成します</summary>
        /// <param name="resource">リソース名</param>
        public Texture(string resource)
            => Init(resource, TextureShrinkMode.Bilinear, TextureMipmapMode.Bilinear, TextureExpansionMode.Bilinear);

        /// <summary>リソース名とミップマップの有無を指定してテクスチャを生成します</summary>
        /// <param name="resource">リソース名</param>
        /// <param name="useMipmap">ミップマップを使用するかどうか</param>
        public Texture(string resource, bool useMipmap)
            => Init(resource, TextureShrinkMode.Bilinear, useMipmap ? TextureMipmapMode.Bilinear : (TextureMipmapMode?)null, TextureExpansionMode.Bilinear);

        /// <summary>リソース名と拡大縮小方法とミップマップのモードを指定してテクスチャを生成します</summary>
        /// <param name="resource">リソース名</param>
        /// <param name="shrinkMode">縮小方法</param>
        /// <param name="mipmapMode">ミップマップのモード</param>
        /// <param name="expansionMode">拡大方法</param>
        public Texture(string resource, TextureShrinkMode shrinkMode, TextureMipmapMode mipmapMode, TextureExpansionMode expansionMode) 
            => Init(resource, shrinkMode, mipmapMode, expansionMode);

        /// <summary>リソース名と拡大縮小方法を指定してミップマップなしのテクスチャを生成します</summary>
        /// <param name="resource">リソース名</param>
        /// <param name="shrinkMode">縮小方法</param>
        /// <param name="expansionMode">拡大方法</param>
        public Texture(string resource, TextureShrinkMode shrinkMode, TextureExpansionMode expansionMode) 
            => Init(resource, shrinkMode, null, expansionMode);
        #endregion

        ~Texture() => Dispose(false);

        /// <summary>テクスチャの一部を更新します</summary>
        /// <param name="newPixels">新しいピクセル配列 (テクスチャ全体のピクセル配列)</param>
        /// <param name="dirtyRegion">変更部分</param>
        internal void UpdateTexture(IntPtr newPixels, Rectangle dirtyRegion)
        {
            GL.BindTexture(TextureTarget.Texture2D, _texture);
            GL.TexSubImage2D(TextureTarget.Texture2D, 0, dirtyRegion.X, dirtyRegion.Y, dirtyRegion.Width, dirtyRegion.Height, PIXEL_FORMAT, PixelType.UnsignedByte, newPixels);
            GL.BindTexture(TextureTarget.Texture2D, Consts.NULL);
        }

        /// <summary>このインスタンスを初期化します</summary>
        /// <param name="resource">リソース名</param>
        /// <param name="shrinkMode">縮小モード</param>
        /// <param name="mipmapMode">ミップマップモード (ミップマップ不使用の場合は null)</param>
        /// <param name="expansionMode">拡大モード</param>
        private void Init(string resource, TextureShrinkMode shrinkMode, TextureMipmapMode? mipmapMode, TextureExpansionMode expansionMode)
        {
            Dispatcher.ThrowIfNotMainThread();
            try {
                using(var pixels = LoadFromResource(resource, out var width, out var height)) {
                    _texture = GL.GenTexture();
                    if(mipmapMode != null) {
                        SetTexture(shrinkMode, mipmapMode.Value, expansionMode, pixels.Ptr, width, height);
                    }
                    else {
                        SetTexture(shrinkMode, expansionMode, pixels.Ptr, width, height);
                    }
                    PixelWidth = width;
                    PixelHeight = height;
                    ShrinkMode = shrinkMode;
                    MipmapMode = mipmapMode;
                    ExpansionMode = expansionMode;
                }
            }
            catch(Exception ex) {
                GL.DeleteTexture(_texture);
                throw ex;
            }
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
        internal static void ReverseYAxis(IntPtr ptr, int width, int height, UnmanagedArray<byte> pixels)
        {
            for(int i = 0; i < height; i++) {
                var row = height - i - 1;
                var head = ptr + width * row * BYTE_PER_PIXEL;
                pixels.CopyFrom(head, i * width * BYTE_PER_PIXEL, width * BYTE_PER_PIXEL);
            }
        }
        #endregion

        #region SetTexture
        /// <summary>ミップマップを生成せずに、バッファに Texture を読み込みます</summary>
        /// <param name="shrinkMode">縮小方法</param>
        /// <param name="expansionMode">拡大方法</param>
        /// <param name="pixels">テクスチャのピクセル配列</param>
        /// <param name="pixelWidth">テクスチャのピクセル幅</param>
        /// <param name="pixelHeight">テクスチャのピクセル高</param>
        private void SetTexture(TextureShrinkMode shrinkMode, TextureExpansionMode expansionMode, IntPtr pixels, int pixelWidth, int pixelHeight)
        {
            GL.BindTexture(TextureTarget.Texture2D, _texture);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, GetMinParameter(shrinkMode));
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, GetMagParameter(expansionMode));
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, pixelWidth, pixelHeight, 0, PIXEL_FORMAT, PixelType.UnsignedByte, pixels);
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
            GL.BindTexture(TextureTarget.Texture2D, Consts.NULL);       // バインド解除
        }

        /// <summary>ミップマップを生成して、バッファに Texture を読み込みます</summary>
        /// <param name="shrinkMode">縮小方法</param>
        /// <param name="mipmapMode">ミップマップのモード</param>
        /// <param name="expansionMode">拡大方法</param>
        /// <param name="pixels">テクスチャのピクセル配列</param>
        /// <param name="pixelWidth">テクスチャのピクセル幅</param>
        /// <param name="pixelHeight">テクスチャのピクセル高</param>
        private void SetTexture(TextureShrinkMode shrinkMode, TextureMipmapMode mipmapMode, TextureExpansionMode expansionMode, IntPtr pixels, int pixelWidth, int pixelHeight)
        {
            GL.BindTexture(TextureTarget.Texture2D, _texture);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, GetMinParameter(shrinkMode, mipmapMode));
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, GetMagParameter(expansionMode));
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, pixelWidth, pixelHeight, 0, PIXEL_FORMAT, PixelType.UnsignedByte, pixels);
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
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
                var pixels = new UnmanagedArray<byte>(bmp.Width * bmp.Height * BYTE_PER_PIXEL);
                ReverseYAxis(bmpData.Scan0, bmp.Width, bmp.Height, pixels);
                bmp.UnlockBits(bmpData);
                return pixels;
            }
        }
        #endregion

        /// <summary>OpenGL に設定するテクスチャの拡大のパラメータを取得します</summary>
        /// <param name="expansionMode">拡大のモード</param>
        /// <returns>OpenGL に設定するテクスチャの拡大のパラメータ</returns>
        private int GetMagParameter(TextureExpansionMode expansionMode)
        {
            switch(expansionMode) {
                case TextureExpansionMode.Bilinear:
                    return (int)TextureMagFilter.Linear;
                case TextureExpansionMode.NearestNeighbor:
                    return (int)TextureMagFilter.Nearest;
                default:
                    throw new ArgumentException();
            }
        }

        /// <summary>OpenGL に設定するテクスチャの縮小のパラメータを取得します</summary>
        /// <param name="shrinkMode">縮小モード</param>
        /// <returns>OpenGL に設定するテクスチャの縮小パラメータ</returns>
        private int GetMinParameter(TextureShrinkMode shrinkMode)
        {
            switch(shrinkMode) {
                case TextureShrinkMode.Bilinear:
                    return (int)TextureMinFilter.Linear;
                case TextureShrinkMode.NearestNeighbor:
                    return (int)TextureMinFilter.Nearest;
                default:
                    throw new ArgumentException();
            }
        }

        /// <summary>OpenGL に設定するテクスチャの縮小とミップマップのパラメータを取得します</summary>
        /// <param name="shrinkMode">縮小のモード</param>
        /// <param name="mipmapMode">ミップマップのモード</param>
        /// <returns>OpenGL に設定するテクスチャの縮小とミップマップのパラメータ</returns>
        private int GetMinParameter(TextureShrinkMode shrinkMode, TextureMipmapMode mipmapMode)
        {
            if(shrinkMode == TextureShrinkMode.Bilinear) {
                if(mipmapMode == TextureMipmapMode.Bilinear) {
                    return (int)TextureMinFilter.LinearMipmapLinear;
                }
                else if(mipmapMode == TextureMipmapMode.NearestNeighbor) {
                    return (int)TextureMinFilter.LinearMipmapNearest;
                }
            }
            else if(shrinkMode == TextureShrinkMode.NearestNeighbor) {
                if(mipmapMode == TextureMipmapMode.Bilinear) {
                    return (int)TextureMinFilter.NearestMipmapLinear;
                }
                else if(mipmapMode == TextureMipmapMode.NearestNeighbor) {
                    return (int)TextureMinFilter.NearestMipmapNearest;
                }
            }
            throw new ArgumentException();
        }

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

    /// <summary>テクスチャの拡大モード</summary>
    public enum TextureExpansionMode
    {
        /// <summary>線形補間</summary>
        Bilinear,
        /// <summary>最近傍補間</summary>
        NearestNeighbor,
    }

    /// <summary>テクスチャの縮小モード</summary>
    public enum TextureShrinkMode
    {
        /// <summary>線形補間</summary>
        Bilinear,
        /// <summary>最近傍補間</summary>
        NearestNeighbor,
    }

    /// <summary>テクスチャのミップマップモード</summary>
    public enum TextureMipmapMode
    {
        /// <summary>線形補間</summary>
        Bilinear,
        /// <summary>最近傍補間</summary>
        NearestNeighbor,
    }
}
