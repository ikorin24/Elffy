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
using Elffy.Effective;

namespace Elffy.Shape
{
    public class Canvas : Plain, IDisposable
    {
        private const int BYTE_PER_PIXEL = 4;
        private Rectangle _dirtyRegion;
        private readonly Bitmap _bmp;
        private readonly Graphics _g;
        private bool _isDisposed;
        private UnmanagedArray<byte> _buf;

        public int PixelWidth => _bmp.Width;
        public int PixelHeight => _bmp.Height;

        public Canvas(Size pixelSize) : this(pixelSize.Width, pixelSize.Height) { }
        public Canvas(int pixelWidth, int pixelHeight)
        {
            _bmp = new Bitmap(pixelWidth, pixelHeight);
            _g = Graphics.FromImage(_bmp);
            Texture = new Texture(pixelWidth, pixelHeight);     // TODO: インスタンス上書き対策
        }

        ~Canvas() => Dispose(false);

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

        public void DrawImage(Bitmap image, Point point)
        {
            if(image == null) { throw new ArgumentNullException(nameof(image)); }
            _g.DrawImage(image, point);
            var point2 = new Point(point.X + image.Width, point.Y + image.Height);
            var updateRegion = GetBounds(point, point2);
            AddDirtyRegion(updateRegion);
        }

        public void Clear(Color color)
        {
            _g.Clear(color);
            AddDirtyRegion(0, 0, _bmp.Width, _bmp.Height);
        }

        protected override void OnRendering()
        {
            // キャンバスの描画に更新部分があった場合、更新部分をテクスチャに反映させます
            if(_dirtyRegion.IsEmpty) { return; }
            var data = _bmp.LockBits(new Rectangle(0, 0, _bmp.Width, _bmp.Height), ImageLockMode.ReadOnly, DPixelFormat.Format32bppArgb);
            if(_buf == null) {
                _buf = new UnmanagedArray<byte>(_bmp.Width * _bmp.Height * BYTE_PER_PIXEL);
            }
            Texture.ReverseYAxis(data.Scan0, _bmp.Width, _bmp.Height, _buf);    // 上下反転
            _bmp.UnlockBits(data);
            Texture?.UpdateTexture(_buf.Ptr, new Rectangle(0, 0, _bmp.Width, _bmp.Height));    // TODO: 差分更新
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
