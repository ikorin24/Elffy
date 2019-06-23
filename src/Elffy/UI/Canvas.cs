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
    public class Canvas : Plain, IDisposable
    {
        private const int BYTE_PER_PIXEL = 4;
        private Rectangle _dirtyRegion;
        private Bitmap _bmp;
        private Graphics _g;
        private bool _isDisposed;
        private byte[] _buf;

        public int PixelWidth => _bmp.Width;
        public int PixelHeight => _bmp.Height;

        public Canvas(Size pixelSize) : this(pixelSize.Width, pixelSize.Height) { }
        public Canvas(int pixelWidth, int pixelHeight)
        {
            _bmp = new Bitmap(pixelWidth, pixelHeight);
            _g = Graphics.FromImage(_bmp);
            Texture = new Texture(pixelWidth, pixelHeight);
        }

        public void DrawString(string text, Font font, Brush brush, PointF point)
        {
            _g.DrawString(text, font, brush, point);
            var size = _g.MeasureString(text, font);
            AddDirtyRegion((int)point.X, (int)point.Y, (int)size.Width + 2, (int)size.Height + 2);
        }

        public void DrawLine(Pen pen, int x1, int y1, int x2, int y2)
        {
            _g.DrawLine(pen, x1, y1, x2, y2);
            var updateRegion = GetBounds(new Point(x1, y1), new Point(x2, y2));
            AddDirtyRegion(updateRegion);
        }

        public void DrawLines(Pen pen, Point[] points)
        {
            if(points == null) { throw new ArgumentNullException(nameof(points)); }
            if(points.Length < 2) { throw new ArgumentException($"Length of {nameof(points)} must be bigger than 2."); }
            _g.DrawLines(pen, points);
            AddDirtyRegion(GetBounds(points));
        }

        public void Clear(Color color)
        {
            _g.Clear(color);
            AddDirtyRegion(0, 0, _bmp.Width, _bmp.Height);
        }

        protected override void OnRendering()
        {
            if(_dirtyRegion.IsEmpty) { return; }
            var data = _bmp.LockBits(new Rectangle(0, 0, _bmp.Width, _bmp.Height), ImageLockMode.ReadOnly, DPixelFormat.Format32bppArgb);
            if(_buf == null) {
                _buf = new byte[_bmp.Width * _bmp.Height * BYTE_PER_PIXEL];
            }
            Texture.ReverseYAxis(data.Scan0, _bmp.Width, _bmp.Height, _buf);    // 上下反転
            _bmp.UnlockBits(data);
            Texture.UpdateTexture(_buf, new Rectangle(0, 0, _bmp.Width, _bmp.Height));
            _dirtyRegion = Rectangle.Empty;
        }

        private Rectangle GetBounds(params Point[] points)
        {
            var minX = int.MaxValue;
            var minY = int.MaxValue;
            var maxX = 0;
            var maxY = 0;
            foreach(var p in points) {
                minX = minX < p.X ? minX : p.X;
                minY = minY < p.Y ? minY : p.Y;
                maxX = maxX > p.X ? maxX : p.X;
                maxY = maxY > p.Y ? maxY : p.Y;
            }
            return new Rectangle(minX, minY, maxX - minX, maxY - minY);
        }

        private void AddDirtyRegion(int x, int y, int width, int height) => AddDirtyRegion(new Rectangle(x, y, width, height));

        private void AddDirtyRegion(Rectangle updateRegion)
        {
            if(_dirtyRegion.IsEmpty == false) {
                updateRegion = Rectangle.Union(_dirtyRegion, updateRegion);
            }
            _dirtyRegion = Rectangle.Intersect(updateRegion, new Rectangle(0, 0, _bmp.Width, _bmp.Height));
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
