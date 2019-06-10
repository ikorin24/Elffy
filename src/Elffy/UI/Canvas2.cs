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
    public class Canvas2 : Renderable, IDisposable
    {
        private const int BYTE_PER_PIXEL = 4;

        private static readonly Vertex[] _vertexArray = new Vertex[4]
        {
            new Vertex(new Vector3(-1, 1, 0),   new Vector3(-1, 1, 0),   Color4.White, new Vector2(-1, 1)),
            new Vertex(new Vector3(-1, -1, 0),  new Vector3(-1, -1, 0),  Color4.White, new Vector2(-1, -1)),
            new Vertex(new Vector3(1, -1, 0),   new Vector3(1, 1, 0),    Color4.White, new Vector2(1, -1)),
            new Vertex(new Vector3(1, 1, 0),    new Vector3(1, -1, 0),   Color4.White, new Vector2(1, 1)),
        };
        private static readonly int[] _indexArray = new int[6] { 0, 1, 2, 2, 3, 0 };
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
            Load(_vertexArray, _indexArray);
            Texture = new Texture(pixelWidth, pixelHeight) { PixelFormat = TexturePixelFormat.Bgra };
        }

        public void DrawString(string text, Font font, Brush brush, PointF point)
        {
            using(var g = Graphics.FromImage(_bmp)) {
                g.DrawString(text, font, brush, point);
            }
            _bmp.Save("hoge.png");
        }

        public void Test()
        {
            using(var g = Graphics.FromImage(_bmp)) {
                g.DrawRectangle(new Pen(Brushes.Green), new Rectangle(0, 0, 80, 90));
            }
            _bmp.Save("hoge.png");
        }

        public void Clear(Color color)
        {
            using(var g = Graphics.FromImage(_bmp)) {
                g.Clear(color);
            }
            _bmp.Save("hoge.png");
        }

        protected override void OnRendering()
        {
            var data = _bmp.LockBits(new Rectangle(0, 0, _bmp.Width, _bmp.Height), ImageLockMode.ReadOnly, DPixelFormat.Format32bppArgb);
            var ptr = data.Scan0;
            if(_buf == null) {
                _buf = new byte[_bmp.Width * _bmp.Height * BYTE_PER_PIXEL];
            }
            Marshal.Copy(ptr, _buf, 0, _buf.Length);
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
                    base.Dispose(true);
                }
                _isDisposed = true;
            }
        }
        #endregion
    }
}
