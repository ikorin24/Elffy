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

        private static readonly Vertex[] _vertexArray = new Vertex[4]
        {
            new Vertex(new Vector3(-1, 1, 0),   new Vector3(-1, 1, 0),   Color4.White, new Vector2(-1, 1)),
            new Vertex(new Vector3(-1, -1, 0),  new Vector3(-1, -1, 0),  Color4.White, new Vector2(-1, -1)),
            new Vertex(new Vector3(1, -1, 0),   new Vector3(1, 1, 0),    Color4.White, new Vector2(1, -1)),
            new Vertex(new Vector3(1, 1, 0),    new Vector3(1, -1, 0),   Color4.White, new Vector2(1, 1)),
        };
        private static readonly int[] _indexArray = new int[6] { 0, 1, 2, 2, 3, 0 };

        #region private Member
        private readonly Bitmap _bmp;
        private readonly Graphics _g;
        private Rectangle _dirtyRegion;
        private bool _isDisposed;
        #endregion

        public int PixelWidth => _bmp.Width;
        public int PixelHeight => _bmp.Height;

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

            _bmp = new Bitmap(pixelWidth, pixelHeight, DPixelFormat.Format32bppArgb);
            _g = Graphics.FromImage(_bmp);
            _g.TextRenderingHint = TextRenderingHint.AntiAlias;

            Load(_vertexArray, _indexArray);

            Texture = new Texture(pixelWidth, pixelHeight);
        }
        #endregion

        #region public Method
        #region Clear
        /// <summary>全体を塗りつぶします</summary>
        /// <param name="color">塗りつぶす色</param>
        public void Clear(Color color)
        {
            _g.Clear(color);
            _dirtyRegion = new Rectangle(0, 0, _bmp.Width, _bmp.Height);
            _bmp.Save("Clear.png");
        }
        #endregion

        #region DrawString
        /// <summary>文字を描画します</summary>
        /// <param name="text">描画する文字列</param>
        /// <param name="font">描画するフォント</param>
        /// <param name="brush">ブラシ</param>
        /// <param name="point">描画を開始する位置</param>
        public void DrawString(string text, Font font, Brush brush, PointF point)       // TODO: DrawStringが上手く反映されない
        {
            _g.DrawString(text, font, brush, point);
            //_bmp.Save("test.png");
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
        public override void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
        #endregion

        #region privage Method
        protected override void OnRendering()
        {
            WriteToBuffer();                // 変更部分をバッファに転写
            if(!_dirtyRegion.IsEmpty && _buf != null) {
                Texture.UpdateTexture(_buf, _dirtyRegion);
                _dirtyRegion = Rectangle.Empty;
            }
        }

        #region WriteToBuffer
        /// <summary>ビットマップのピクセルの変更部分をバッファに転写</summary>
        private void WriteToBuffer()
        {
            // バッファは必要な部分だけしか参照されず、該当部分はきちんと更新されているので使いまわせる

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
                _bmp.UnlockBits(data);
            }
        }
        #endregion

        #region Dispose
        protected override void Dispose(bool manual)
        {
            if(!_isDisposed) {
                if(manual) {
                    _bmp.Dispose();
                    _g.Dispose();
                    _buf = null;
                }
                base.Dispose(manual);
                _isDisposed = true;
            }
        }
        #endregion
        #endregion
    }
}
