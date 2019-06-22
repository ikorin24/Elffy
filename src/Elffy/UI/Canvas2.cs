using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DPixelFormat = System.Drawing.Imaging.PixelFormat;
using Elffy;
using Elffy.Core;
using OpenTK;
using OpenTK.Graphics;
using System.Runtime.InteropServices;

namespace Elffy.UI
{
    public class Canvas2 : Plain, IDisposable
    {
        private const int BYTE_PER_PIXEL = 4;

        private Bitmap _bmp;
        private Graphics _g;
        private bool _isDisposed;
        private byte[] _buf;

        public int PixelWidth => _bmp.Width;
        public int PixelHeight => _bmp.Height;

        public Canvas2(Size pixelSize) : this(pixelSize.Width, pixelSize.Height) { }
        public Canvas2(int pixelWidth, int pixelHeight)
        {
            _bmp = new Bitmap(pixelWidth, pixelHeight);
            _g = Graphics.FromImage(_bmp);
            Texture = new Texture(pixelWidth, pixelHeight);
        }

        public void DrawString(string text, Font font, Brush brush, PointF point)
        {
            using(var g = Graphics.FromImage(_bmp)) {
                g.DrawString(text, font, brush, point);
            }
        }

        public void Clear(Color color)
        {
            using(var g = Graphics.FromImage(_bmp)) {
                g.Clear(color);
            }
        }

        protected override void OnRendering()
        {
            var data = _bmp.LockBits(new Rectangle(0, 0, _bmp.Width, _bmp.Height), ImageLockMode.ReadOnly, DPixelFormat.Format32bppArgb);
            var ptr = data.Scan0;
            if(_buf == null) {
                _buf = new byte[_bmp.Width * _bmp.Height * BYTE_PER_PIXEL];
            }
            Texture.ReverseYAxis(data.Scan0, _bmp.Width, _bmp.Height, _buf);    // 上下反転
            _bmp.UnlockBits(data);
            Texture.UpdateTexture(_buf, new Rectangle(0, 0, _bmp.Width, _bmp.Height));
        }

        #region Dispose
        public override void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

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
    }
}
