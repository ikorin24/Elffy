using OpenTK.Graphics.OpenGL;
using System;
using Elffy.Core;
using System.Drawing;
using System.Drawing.Imaging;
using Elffy.Threading;
using Elffy.Effective;
using System.Threading.Tasks;
using Elffy.Exceptions;
using System.Linq;
using Elffy.Core.MetaFile;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

namespace Elffy
{
    /// <summary><see cref="Renderable"/> に適用する変更不能なテクスチャクラス</summary>
    public sealed class Texture : TextureBase, IDisposable
    {
        private bool _disposed;
        /// <summary>OpenGL の Texture のバッファ識別番号</summary>
        private int _textureBuffer;
        /// <summary>OpenGL の Texture バッファにデータが読み込まれているかどうか</summary>
        private bool IsLoaded => _textureBuffer != Consts.NULL;

        #region constructor
        /// <summary>ミップマップありのテクスチャを生成します</summary>
        private Texture()
            : this(TextureShrinkMode.Bilinear, TextureMipmapMode.Bilinear, TextureExpansionMode.Bilinear) { }

        /// <summary>ミップマップの有無を指定してテクスチャを生成します</summary>
        /// <param name="useMipmap">ミップマップを使用するかどうか</param>
        private Texture(bool useMipmap)
            : this(TextureShrinkMode.Bilinear, useMipmap ? TextureMipmapMode.Bilinear : TextureMipmapMode.None, TextureExpansionMode.Bilinear) { }

        /// <summary>拡大縮小方法を指定してミップマップなしのテクスチャを生成します</summary>
        /// <param name="shrinkMode">縮小方法</param>
        /// <param name="expansionMode">拡大方法</param>
        private Texture(TextureShrinkMode shrinkMode, TextureExpansionMode expansionMode)
            : this(shrinkMode, TextureMipmapMode.None, expansionMode) { }

        /// <summary>拡大縮小方法とミップマップのモードを指定してテクスチャを生成します</summary>
        /// <param name="shrinkMode">縮小方法</param>
        /// <param name="mipmapMode">ミップマップのモード</param>
        /// <param name="expansionMode">拡大方法</param>
        private Texture(TextureShrinkMode shrinkMode, TextureMipmapMode mipmapMode, TextureExpansionMode expansionMode)
            : base(shrinkMode, mipmapMode, expansionMode) { }
        #endregion

        ~Texture() => Dispose(false);

        /// <summary>リソースからテクスチャロードします</summary>
        /// <param name="resource">リソース名</param>
        /// <returns>ロードしたテクスチャ</returns>
        public static Texture LoadFrom(string resource)
        {
            var texture = new Texture();
            var pixels = LoadResourceBitmap(resource, out int w, out int h);
            texture.PixelWidth = w;
            texture.PixelHeight = h;
            Dispatcher.Invoke(() =>
            {
                using(pixels) {
                    texture.SetPixels(pixels.Ptr);
                }
            });
            return texture;
        }

        /// <summary>非同期でリソースからテクスチャをロードします</summary>
        /// <param name="resource">リソース名</param>
        /// <returns>ロードしたテクスチャ</returns>
        public static async Task<Texture> LoadFromAsync(string resource)
        {
            return await NonCaptureContextTaskFactory.StartNew(() => LoadFrom(resource));
        }

        /// <summary>複数画像を内部に持つ画像リソースから、それらのテクスチャをロードします</summary>
        /// <param name="spriteData">スプライト情報</param>
        /// <returns>ロードしたテクスチャ配列</returns>
        internal static Texture[] LoadFrom(SpriteMetadata spriteData)
        {
            ExceptionManager.ThrowIfNullArg(spriteData, nameof(spriteData));
            using(var stream = Resources.GetStream(spriteData.TextureResource))
            using(var bmp = new Bitmap(stream)) {
                var textures = new Texture[spriteData.PageCount];

                // 1枚の画像に統合されている複数枚の画像を分離する
                var images = Enumerable.Range(0, spriteData.PageCount)
                                     .Select(i => 
                {
                    var rect = new Rectangle(i % spriteData.XCount * spriteData.PixelWidth,
                                               i / spriteData.XCount * spriteData.PixelHeight,
                                               spriteData.PixelWidth,
                                               spriteData.PixelHeight);
                    var pixels = new UnmanagedArray<byte>(rect.Width * rect.Height * BYTE_PER_PIXEL);
                    using(var subBmp = bmp.Clone(rect, PixelFormat.Format32bppPArgb)) {
                        subBmp.RotateFlip(RotateFlipType.RotateNoneFlipY);
                        var bmpData = subBmp.LockBits(new Rectangle(0, 0, subBmp.Width, subBmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppPArgb);
                        pixels.CopyFrom(bmpData.Scan0, 0, pixels.Length);
                        subBmp.UnlockBits(bmpData);
                    }
                    var texture = new Texture();
                    texture.PixelWidth = rect.Width;
                    texture.PixelHeight = rect.Height;
                    return (i, texture, pixels);
                });

                // 各テクスチャにピクセルをセットする
                foreach(var (i, texture, pixels) in images) {
                    textures[i] = texture;
                    Dispatcher.Invoke(() =>
                    {
                        using(pixels) {
                            texture.SetPixels(pixels.Ptr);
                        }
                    });
                }
                return textures;
            }
        }

        /// <summary>拡大縮小方法とミップマップのモードを指定して空のテクスチャを生成します</summary>
        /// <param name="shrinkMode">縮小方法</param>
        /// <param name="mipmapMode">ミップマップのモード</param>
        /// <param name="expansionMode">拡大方法</param>
        internal static Texture GenerateEmpty(TextureShrinkMode shrinkMode, TextureMipmapMode mipmapMode, TextureExpansionMode expansionMode)
        {
            return new Texture(shrinkMode, mipmapMode, expansionMode);
        }

        /// <summary>現在のOpenGLのTextureをこのインスタンスのテクスチャに切り替えます</summary>
        internal override void SwitchBind()
        {
            GL.BindTexture(TextureTarget.Texture2D, _textureBuffer);
        }

        #region private Method
        /// <summary>画像リソースのピクセル配列を取得します</summary>
        /// <param name="resource">リソース名</param>
        /// <param name="pixelWidth">ピクセル幅</param>
        /// <param name="pixelHeight">ピクセル高</param>
        /// <returns>ピクセル配列</returns>
        private static UnmanagedArray<byte> LoadResourceBitmap(string resource, out int pixelWidth, out int pixelHeight)
        {
            using(var stream = Resources.GetStream(resource))
            using(var bmp = new Bitmap(stream)) {
                bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);
                var pixels = new UnmanagedArray<byte>(bmp.Width * bmp.Height * BYTE_PER_PIXEL);
                var bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                                           ImageLockMode.ReadOnly,
                                           PixelFormat.Format32bppPArgb);
                pixels.CopyFrom(bmpData.Scan0, 0, pixels.Length);
                bmp.UnlockBits(bmpData);
                pixelWidth = bmp.Width;
                pixelHeight = bmp.Height;
                return pixels;
            }
        }

        /// <summary>OpenGL のテクスチャバッファを確保してピクセル配列を送ります</summary>
        /// <param name="pixels">ピクセル配列</param>
        private void SetPixels(IntPtr pixels)
        {
            Dispatcher.ThrowIfNotMainThread();
            if(IsLoaded) { throw new InvalidOperationException("Pixels are already loaded."); }
            if(_disposed) { throw new ObjectDisposedException(nameof(TextureBase)); }
            try {
                _textureBuffer = GL.GenTexture();
                SetTexture(_textureBuffer, ShrinkMode, MipmapMode, ExpansionMode, pixels, PixelWidth, PixelHeight);
            }
            catch(Exception ex) {
                GL.DeleteTexture(_textureBuffer);
                _textureBuffer = Consts.NULL;
                throw ex;
            }
        }
        #endregion

        #region Dispose
        /// <summary><see cref="IDisposable.Dispose"/> 実装</summary>
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
                Dispatcher.Invoke(() => GL.DeleteTexture(_textureBuffer));
                _disposed = true;
            }
        }
        #endregion
    }
}
