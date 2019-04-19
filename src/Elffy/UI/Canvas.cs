using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Text;
using System.Drawing;
using System.Linq;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using DPixelFormat = System.Drawing.Imaging.PixelFormat;
using Elffy.Core;

namespace Elffy.UI
{
    public class Canvas : Renderable, IDisposable
    {
        const int BYTE_PER_PIXEL = 4;
        /// <summary>テクスチャ更新時に使用するバッファ(メンバ変数として使いまわすことでGCを削減)</summary>
        private byte[] _buf;

        #region private Member
        private readonly Bitmap _bmp;
        private readonly Graphics _g;
        private readonly int _texture;
        private Rectangle _dirtyRegion;
        private bool _isDisposed;
        #endregion

        public int PixelWidth => _bmp.Width;
        public int PixelHeight => _bmp.Height;

        #region Texture
        /// <summary>テクスチャ番号</summary>
        internal int Texture
        {
            get
            {
                UpdateTexture();
                return _texture;
            }
        }
        #endregion

        #region constructor
        /// <summary>コンストラクタ</summary>
        /// <param name="pixelSize">ピクセルサイズ</param>
        public Canvas(Size pixelSize) : this(pixelSize.Width, pixelSize.Height) { }

        /// <summary>コンストラクタ</summary>
        /// <param name="pixelWidth">ピクセル幅</param>
        /// <param name="pixelHeight">ピクセル高さ</param>
        public Canvas(int pixelWidth, int pixelHeight)
        {
            if(pixelWidth <= 0) { throw new ArgumentException(nameof(pixelWidth)); }
            if(pixelHeight <= 0) { throw new ArgumentException(nameof(pixelHeight)); }
            if(GraphicsContext.CurrentContext == null) { throw new InvalidOperationException("No GraphicsContext is current on the calling thread."); }

            _bmp = new Bitmap(pixelWidth, pixelHeight, DPixelFormat.Format32bppArgb);
            _g = Graphics.FromImage(_bmp);
            _g.TextRenderingHint = TextRenderingHint.AntiAlias;
            _texture = GL.GenTexture();

            GL.BindTexture(TextureTarget.Texture2D, _texture);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, pixelWidth, pixelHeight, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
        }
        #endregion

        #region public Method
        #region Destroy
        public override void Destroy()
        {
            base.Destroy();
            Dispose();
        }
        #endregion

        #region Clear
        /// <summary>全体を塗りつぶします</summary>
        /// <param name="color">塗りつぶす色</param>
        public void Clear(Color color)
        {
            _g.Clear(color);
            _dirtyRegion = new Rectangle(0, 0, _bmp.Width, _bmp.Height);
        }
        #endregion

        #region DrawString
        /// <summary>文字を描画します</summary>
        /// <param name="text">描画する文字列</param>
        /// <param name="font">描画するフォント</param>
        /// <param name="brush">ブラシ</param>
        /// <param name="point">描画を開始する位置</param>
        public void DrawString(string text, Font font, Brush brush, PointF point)
        {
            _g.DrawString(text, font, brush, point);

            // 描画した範囲を記録
            var size = _g.MeasureString(text, font);
            var updateRegion = new Rectangle((int)point.X, (int)point.Y, (int)size.Width + 2, (int)size.Height + 2);
            if(_dirtyRegion.IsEmpty == false) {
                updateRegion = Rectangle.Union(_dirtyRegion, updateRegion);
            }
            _dirtyRegion = Rectangle.Intersect(updateRegion, new Rectangle(0, 0, _bmp.Width, _bmp.Height));
        }
        #endregion

        #region Dispose
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
        #endregion

        public override void Render()
        {
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadMatrix(ref _modelView);
            var cameraMatrix = Camera.Current.Matrix;
            GL.MultMatrix(ref cameraMatrix);

            GL.BindTexture(TextureTarget.Texture2D, Texture);
            GL.Begin(PrimitiveType.Quads);
            GL.TexCoord2(0.0f, 1.0f); GL.Vertex2(-1f, -1f);
            GL.TexCoord2(1.0f, 1.0f); GL.Vertex2(1f, -1f);
            GL.TexCoord2(1.0f, 0.0f); GL.Vertex2(1f, 1f);
            GL.TexCoord2(0.0f, 0.0f); GL.Vertex2(-1f, 1f);
            GL.End();
        }

        #region privage Method
        #region UpdateTexture
        /// <summary>変更部分のテクスチャを更新</summary>
        private void UpdateTexture()
        {
            // 変更部分がある時のみ更新
            if(_dirtyRegion != RectangleF.Empty) {
                var data = _bmp.LockBits(new Rectangle(0, 0, _bmp.Width, _bmp.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, DPixelFormat.Format32bppArgb);
                var subStartAddr = data.Scan0 + (_dirtyRegion.Y * _bmp.Width + _dirtyRegion.X) * BYTE_PER_PIXEL;
                if(_buf == null) {
                    _buf = new byte[_bmp.Width * _bmp.Height * BYTE_PER_PIXEL]; // バッファが用意されていないなら用意する。(バッファ長はビットマップ全体の大きさを用意)
                }
                for(int i = 0; i < _dirtyRegion.Height; i++) {
                    var addr = subStartAddr + i * _bmp.Width * BYTE_PER_PIXEL;  // コピー元のアドレス
                    var len = _dirtyRegion.Width * BYTE_PER_PIXEL;              // コピーする長さ
                    var bufPos = i * _dirtyRegion.Width * BYTE_PER_PIXEL;       // コピー先の配列の位置
                    Marshal.Copy(addr, _buf, bufPos, len);
                }

                GL.BindTexture(TextureTarget.Texture2D, _texture);
                GL.TexSubImage2D(TextureTarget.Texture2D, 0,
                    _dirtyRegion.X, _dirtyRegion.Y, _dirtyRegion.Width, _dirtyRegion.Height,
                    PixelFormat.Bgra, PixelType.UnsignedByte, _buf);            // バッファは必要な部分だけしか参照されず、該当部分はきちんと更新されているので使いまわせる
                _bmp.UnlockBits(data);
                _dirtyRegion = Rectangle.Empty;
            }
        }
        #endregion

        #region Dispose
        private void Dispose(bool manual)
        {
            if(!_isDisposed) {
                if(manual) {
                    _bmp.Dispose();
                    _g.Dispose();
                    if(GraphicsContext.CurrentContext != null) { GL.DeleteTexture(_texture); }
                }

                _isDisposed = true;
            }
        }
        #endregion
        #endregion
    }
}
