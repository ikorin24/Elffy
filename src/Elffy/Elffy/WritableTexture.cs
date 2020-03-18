#nullable enable
using OpenTK.Graphics.OpenGL;
using System;
using Elffy.Core;
using System.Drawing;
using System.Drawing.Imaging;
using Elffy.Threading;
using System.Collections.Generic;
using Elffy.Exceptions;
using System.Runtime.CompilerServices;

namespace Elffy
{
    /// <summary>上書き描画可能なテクスチャクラス</summary>
    public sealed class WritableTexture : TextureBase, IDisposable
    {
        private bool _disposed;
        /// <summary>OpenGL の Texture のバッファ識別番号</summary>
        private int _textureBuffer;
        /// <summary>OpenGL の Texture バッファにデータが読み込まれているかどうか</summary>
        private bool IsLoaded => _textureBuffer != Consts.NULL;
        private Bitmap _bitmap = null!;
        private Graphics _g = null!;
        private Rectangle _dirtyRegion;

        #region constructor
        /// <summary>上書き描画可能な白色テクスチャを生成します</summary>
        /// <param name="width">テクスチャ幅</param>
        /// <param name="height">テクスチャ高</param>
        public WritableTexture(int width, int height)
            : this(width, height, Color.White, TextureShrinkMode.NearestNeighbor, TextureMipmapMode.NearestNeighbor, TextureExpansionMode.NearestNeighbor) { }

        /// <summary>上書き描画可能なテクスチャを生成します</summary>
        /// <param name="width">テクスチャ幅</param>
        /// <param name="height">テクスチャ高</param>
        /// <param name="background">背景色</param>
        public WritableTexture(int width, int height, Color background)
            : this(width, height, background, TextureShrinkMode.NearestNeighbor, TextureMipmapMode.NearestNeighbor, TextureExpansionMode.NearestNeighbor) { }

        /// <summary>上書き描画可能なテクスチャを生成します</summary>
        /// <param name="width">ピクセル幅</param>
        /// <param name="height">ピクセル高</param
        /// <param name="background">背景色</param>
        /// <param name="shrinkMode">テクスチャの縮小モード</param>
        /// <param name="mipmapMode">テクスチャのミップマップモード</param>
        /// <param name="expansionMode">テクスチャの拡大モード</param>
        public WritableTexture(int width, int height, Color background, TextureShrinkMode shrinkMode, TextureMipmapMode mipmapMode, TextureExpansionMode expansionMode) 
            : base(shrinkMode, mipmapMode, expansionMode)
        {
            ArgumentChecker.ThrowOutOfRangeIf(width <= 0, nameof(width), width, $"{nameof(width)} is out of range");
            ArgumentChecker.ThrowOutOfRangeIf(height <= 0, nameof(height), height, $"{nameof(height)} is out of range");
            _bitmap = new Bitmap(width, height);
            _g = Graphics.FromImage(_bitmap);
            _g.Clear(background);
            PixelWidth = _bitmap.Width;
            PixelHeight = _bitmap.Height;
            CurrentScreen.Dispatcher.Invoke(() =>
            {
                var bmpData = _bitmap.LockBits(new Rectangle(0, 0, _bitmap.Width, _bitmap.Height),
                                               ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
                SetPixels(bmpData.Scan0);
                _bitmap.UnlockBits(bmpData);
            });
        }
        #endregion

        ~WritableTexture() => Dispose(false);

        /// <summary>位置とフォントを指定して文字列を描画します</summary>
        /// <param name="text">描画する文字列</param>
        /// <param name="font">フォント</param>
        /// <param name="point">描画位置</param>
        public void DrawString(string text, Font font, PointF point)
        {
            CurrentScreen.Dispatcher.ThrowIfNotMainThread();
            ThrowIfDisposed();
            ArgumentChecker.ThrowIfNullArg(font, nameof(font));
            _g.DrawString(text, font, Brushes.Green, point.X, point.Y);
            var size = _g.MeasureString(text, font);
            AddDirtyRegion(new Rectangle((int)point.X, (int)point.Y, (int)size.Width + 2, (int)size.Height + 2));
        }

        /// <summary>ペンと座標を指定して線を描画します</summary>
        /// <param name="pen">線を描画するペン</param>
        /// <param name="x1">点1のX座標</param>
        /// <param name="y1">点1のY座標</param>
        /// <param name="x2">点2のX座標</param>
        /// <param name="y2">点2のY座標</param>
        public void DrawLine(Pen pen, int x1, int y1, int x2, int y2)
        {
            CurrentScreen.Dispatcher.ThrowIfNotMainThread();
            ThrowIfDisposed();
            ArgumentChecker.ThrowIfNullArg(pen, nameof(pen));
            _g.DrawLine(pen, x1, y1, x2, y2);
            var updateRegion = GetBounds(new Point(x1, y1), new Point(x2, y2));
            AddDirtyRegion(updateRegion);
        }

        /// <summary>ペンと座標を指定してポリラインを描画します</summary>
        /// <param name="pen"></param>
        /// <param name="points"></param>
        public void DrawLines(Pen pen, Point[] points)
        {
            CurrentScreen.Dispatcher.ThrowIfNotMainThread();
            ThrowIfDisposed();
            ArgumentChecker.ThrowIfNullArg(pen, nameof(pen));
            ArgumentChecker.ThrowIfNullArg(points, nameof(points));
            ArgumentChecker.ThrowArgumentIf(points.Length < 2, $"Length of {nameof(points)} must be bigger than 2.");
            _g.DrawLines(pen, points);
            AddDirtyRegion(GetBounds(points));
        }

        /// <summary>指定した座標に画像を描画します</summary>
        /// <param name="image">描画する画像</param>
        /// <param name="point">座標</param>
        public void DrawImage(Bitmap image, Point point)
        {
            CurrentScreen.Dispatcher.ThrowIfNotMainThread();
            ThrowIfDisposed();
            ArgumentChecker.ThrowIfNullArg(image, nameof(image));
            _g.DrawImage(image, point);
            var point2 = new Point(point.X + image.Width, point.Y + image.Height);
            var updateRegion = GetBounds(point, point2);
            AddDirtyRegion(updateRegion);
        }

        /// <summary>指定した色でテクスチャを塗りつぶします</summary>
        /// <param name="color">テクスチャを塗りつぶす色</param>
        public void Clear(Color color)
        {
            CurrentScreen.Dispatcher.ThrowIfNotMainThread();
            ThrowIfDisposed();
            _g.Clear(color);
            AddDirtyRegion(new Rectangle(0, 0, _bitmap.Width, _bitmap.Height));
        }

        internal override void Apply()
        {
            base.Apply();

            // 変更されていた場合、変更をOpenGLに送信
            if(!_dirtyRegion.IsEmpty) {
                var bmpData = _bitmap.LockBits(_dirtyRegion, ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
                GL.TexSubImage2D(TextureTarget.Texture2D, 0, _dirtyRegion.X, _dirtyRegion.Y, _dirtyRegion.Width, _dirtyRegion.Height, PIXEL_FORMAT, PixelType.UnsignedByte, bmpData.Scan0);
                _bitmap.UnlockBits(bmpData);
                _dirtyRegion = Rectangle.Empty;
            }
        }

        protected internal override int TextureID => _textureBuffer;

        #region private Method
        /// <summary>OpenGL のテクスチャバッファを確保してピクセル配列を送ります</summary>
        /// <remarks>※メインスレッドから呼び出してください</remarks>
        /// <param name="pixels">ピクセル配列</param>
        private void SetPixels(IntPtr pixels)
        {
            // メインスレッドから呼んでください

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

        /// <summary>複数点を囲む矩形を取得します</summary>
        /// <param name="points">複数の点</param>
        /// <returns>矩形</returns>
        private Rectangle GetBounds(ReadOnlySpan<Point> points)
        {
            var minX = int.MaxValue;
            var minY = int.MaxValue;
            var maxX = int.MinValue;
            var maxY = int.MinValue;
            foreach(var p in points) {
                minX = minX < p.X ? minX : p.X;
                minY = minY < p.Y ? minY : p.Y;
                maxX = maxX > p.X ? maxX : p.X;
                maxY = maxY > p.Y ? maxY : p.Y;
            }
            return new Rectangle(minX, minY, maxX - minX, maxY - minY);
        }

        /// <summary>2点を囲む矩形を取得します</summary>
        /// <param name="point1">点1</param>
        /// <param name="point2">点2</param>
        /// <returns>矩形</returns>
        private Rectangle GetBounds(Point point1, Point point2)
        {
            var minX = point1.X < point2.X ? point1.X : point2.X;
            var minY = point1.Y < point2.Y ? point1.Y : point2.Y;
            var maxX = point1.X > point2.X ? point1.X : point2.X;
            var maxY = point1.Y > point2.Y ? point1.Y : point2.Y;
            return new Rectangle(minX, minY, maxX - minX, maxY - minY);
        }

        /// <summary>描画によって変更された部分を囲う矩形を追加します</summary>
        /// <param name="updateRegion"></param>
        private void AddDirtyRegion(Rectangle updateRegion)
        {
            if(_dirtyRegion.IsEmpty == false) {
                updateRegion = Rectangle.Union(_dirtyRegion, updateRegion);
            }
            _dirtyRegion = Rectangle.Intersect(updateRegion, new Rectangle(0, 0, _bitmap.Width, _bitmap.Height));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ThrowIfDisposed()
        {
            if(_disposed) { throw new ObjectDisposedException(nameof(WritableTexture)); }
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
                    _bitmap.Dispose();
                    _bitmap = null!;
                    _g.Dispose();
                    _g = null!;
                }
                // Release unmanaged resource here.
                CurrentScreen.Dispatcher.Invoke(() => GL.DeleteTexture(_textureBuffer));
                _disposed = true;
            }
        }
        #endregion
    }
}
