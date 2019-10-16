using OpenTK.Graphics.OpenGL;
using System;
using Elffy.Core;
using System.Drawing;
using Elffy.Threading;
using Elffy.Effective;

namespace Elffy
{
    #region class Texture
    /// <summary><see cref="Renderable"/> に適用するのテクスチャクラス</summary>
    public sealed class Texture : IDisposable
    {
        private const int BYTE_PER_PIXEL = 4;
        /// <summary>ピクセル配列のピクセルの</summary>
        private const PixelFormat PIXEL_FORMAT = PixelFormat.Bgra;
        private bool _disposed;
        /// <summary>OpenGL の Texture のバッファ識別番号</summary>
        private int _textureBuffer;

        #region Property
        /// <summary>テクスチャの縮小モードを取得します</summary>
        public TextureShrinkMode ShrinkMode { get; }

        /// <summary>テクスチャの拡大モードを取得します</summary>
        public TextureExpansionMode ExpansionMode { get; }
        /// <summary>テクスチャのミップマップモードを取得します (ミップマップ不使用の場合は null)</summary>
        public TextureMipmapMode MipmapMode { get; }
        /// <summary>テクスチャがミップマップを持っているかどうかを取得します</summary>
        public bool HasMipmap => MipmapMode != TextureMipmapMode.None;
        /// <summary>テクスチャのピクセル幅を取得します</summary>
        public int PixelWidth { get; }
        /// <summary>テクスチャのピクセル高さを取得します</summary>
        public int PixelHeight { get; }
        #endregion

        #region constructor
        public Texture(int width, int height) : this(width, height, Color.White) { }

        /// <summary>ピクセルサイズを指定して指定色で塗りつぶしたテクスチャを生成します</summary>
        /// <param name="width">テクスチャのピクセル幅</param>
        /// <param name="height">テクスチャのピクセル高</param>
        /// <param name="fill">塗りつぶし色</param>
        public Texture(int width, int height, Color fill)
        {
            if(width <= 0) { throw new ArgumentOutOfRangeException(nameof(width)); }
            if(height <= 0) { throw new ArgumentOutOfRangeException(nameof(height)); }
            Dispatcher.ThrowIfNotMainThread();
            try {
                var pixelCount = width * height;
                using(var pixels = new UnmanagedArray<byte>(pixelCount * BYTE_PER_PIXEL)) {
                    for(int i = 0; i < pixelCount; i++) {
                        pixels[i * BYTE_PER_PIXEL + 0] = fill.B;
                        pixels[i * BYTE_PER_PIXEL + 1] = fill.G;
                        pixels[i * BYTE_PER_PIXEL + 2] = fill.R;
                        pixels[i * BYTE_PER_PIXEL + 3] = fill.A;
                    }
                    _textureBuffer = GL.GenTexture();
                    SetTexture(TextureShrinkMode.Bilinear, TextureMipmapMode.None, TextureExpansionMode.Bilinear, pixels.Ptr, width, height);
                }
            }
            catch(Exception ex) {
                GL.DeleteTexture(_textureBuffer);
                throw ex;
            }
        }

        /// <summary>リソース名を指定してミップマップありのテクスチャを生成します</summary>
        /// <param name="resource">リソース名</param>
        public Texture(string resource)
            : this(resource, TextureShrinkMode.Bilinear, TextureMipmapMode.Bilinear, TextureExpansionMode.Bilinear) { }

        /// <summary>リソース名とミップマップの有無を指定してテクスチャを生成します</summary>
        /// <param name="resource">リソース名</param>
        /// <param name="useMipmap">ミップマップを使用するかどうか</param>
        public Texture(string resource, bool useMipmap)
            : this(resource, TextureShrinkMode.Bilinear, useMipmap ? TextureMipmapMode.Bilinear : TextureMipmapMode.None, TextureExpansionMode.Bilinear) { }

        /// <summary>リソース名と拡大縮小方法を指定してミップマップなしのテクスチャを生成します</summary>
        /// <param name="resource">リソース名</param>
        /// <param name="shrinkMode">縮小方法</param>
        /// <param name="expansionMode">拡大方法</param>
        public Texture(string resource, TextureShrinkMode shrinkMode, TextureExpansionMode expansionMode)
            : this(resource, shrinkMode, TextureMipmapMode.None, expansionMode) { }

        /// <summary>リソース名と拡大縮小方法とミップマップのモードを指定してテクスチャを生成します</summary>
        /// <param name="resource">リソース名</param>
        /// <param name="shrinkMode">縮小方法</param>
        /// <param name="mipmapMode">ミップマップのモード</param>
        /// <param name="expansionMode">拡大方法</param>
        public Texture(string resource, TextureShrinkMode shrinkMode, TextureMipmapMode mipmapMode, TextureExpansionMode expansionMode)
        {
            Dispatcher.ThrowIfNotMainThread();
            try {
                using(var pixels = LoadFromResource(resource, out var width, out var height)) {
                    _textureBuffer = GL.GenTexture();
                    SetTexture(shrinkMode, mipmapMode, expansionMode, pixels.Ptr, width, height);
                    PixelWidth = width;
                    PixelHeight = height;
                    ShrinkMode = shrinkMode;
                    MipmapMode = mipmapMode;
                    ExpansionMode = expansionMode;
                }
            }
            catch(Exception ex) {
                GL.DeleteTexture(_textureBuffer);
                throw ex;
            }
        }
        #endregion

        ~Texture() => Dispose(false);

        /// <summary>テクスチャの一部を更新します</summary>
        /// <param name="newPixels">新しいピクセル配列 (テクスチャ全体のピクセル配列)</param>
        /// <param name="dirtyRegion">変更部分</param>
        public void UpdateTexture(IntPtr newPixels, Rectangle dirtyRegion)
        {
            Dispatcher.ThrowIfNotMainThread();
            GL.BindTexture(TextureTarget.Texture2D, _textureBuffer);
            GL.TexSubImage2D(TextureTarget.Texture2D, 0, dirtyRegion.X, dirtyRegion.Y, dirtyRegion.Width, dirtyRegion.Height, PIXEL_FORMAT, PixelType.UnsignedByte, newPixels);
            GL.BindTexture(TextureTarget.Texture2D, Consts.NULL);
        }

        /// <summary>
        /// 現在のOpenGLのTextureをこのインスタンスのテクスチャに切り替えます<para/>
        /// ※ 必要な操作を終えた後、必ず GL.BindTexture(TextureTarget.Texture2D, 0) でバインド解除をしてください。<para/>
        /// </summary>
        internal void SwitchBind()
        {
            GL.BindTexture(TextureTarget.Texture2D, _textureBuffer);
        }

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

        /// <summary>ミップマップを生成して、バッファに Texture を読み込みます</summary>
        /// <param name="shrinkMode">縮小方法</param>
        /// <param name="mipmapMode">ミップマップのモード</param>
        /// <param name="expansionMode">拡大方法</param>
        /// <param name="pixels">テクスチャのピクセル配列</param>
        /// <param name="pixelWidth">テクスチャのピクセル幅</param>
        /// <param name="pixelHeight">テクスチャのピクセル高</param>
        private void SetTexture(TextureShrinkMode shrinkMode, TextureMipmapMode mipmapMode, TextureExpansionMode expansionMode, IntPtr pixels, int pixelWidth, int pixelHeight)
        {
            GL.BindTexture(TextureTarget.Texture2D, _textureBuffer);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, GetMinParameter(shrinkMode, mipmapMode));
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, GetMagParameter(expansionMode));
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, pixelWidth, pixelHeight, 0, PIXEL_FORMAT, PixelType.UnsignedByte, pixels);
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
            GL.BindTexture(TextureTarget.Texture2D, Consts.NULL);       // バインド解除
        }

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

        /// <summary>OpenGL に設定するテクスチャの縮小とミップマップのパラメータを取得します</summary>
        /// <param name="shrinkMode">縮小のモード</param>
        /// <param name="mipmapMode">ミップマップのモード</param>
        /// <returns>OpenGL に設定するテクスチャの縮小とミップマップのパラメータ</returns>
        private int GetMinParameter(TextureShrinkMode shrinkMode, TextureMipmapMode mipmapMode)
        {
            switch(shrinkMode) {
                case TextureShrinkMode.Bilinear:
                    switch(mipmapMode) {
                        case TextureMipmapMode.None:
                            return (int)TextureMinFilter.Linear;
                        case TextureMipmapMode.Bilinear:
                            return (int)TextureMinFilter.LinearMipmapLinear;
                        case TextureMipmapMode.NearestNeighbor:
                            return (int)TextureMinFilter.LinearMipmapNearest;
                    }
                    break;
                case TextureShrinkMode.NearestNeighbor:
                    switch(mipmapMode) {
                        case TextureMipmapMode.None:
                            return (int)TextureMinFilter.Nearest;
                        case TextureMipmapMode.Bilinear:
                            return (int)TextureMinFilter.NearestMipmapLinear;
                        case TextureMipmapMode.NearestNeighbor:
                            return (int)TextureMinFilter.NearestMipmapNearest;
                    }
                    break;
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
                var texture = _textureBuffer;
                Dispatcher.Invoke(() => { GL.DeleteTexture(_textureBuffer); });
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
        /// <summary>ミップマップを使用しません</summary>
        None,
        /// <summary>線形補間</summary>
        Bilinear,
        /// <summary>最近傍補間</summary>
        NearestNeighbor,
    }
}
