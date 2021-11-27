#nullable enable
using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Elffy.Effective;
using System.Runtime.CompilerServices;

namespace Elffy.Imaging
{
    [DebuggerDisplay("{DebugView,nq}")]
    internal unsafe sealed class DefaultImageSource : IImageSource, IChainInstancePooled<DefaultImageSource>
    {
        private static Int16TokenFactory _tokenFactory;

        private int _width;
        private int _height;
        private ColorByte* _pixels;
        private short _token;
        private DefaultImageSource? _nextPooled;

        public int Width => _width;
        public int Height => _height;
        public short Token => _token;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebugView => $"(Token = {_token})";

        public ColorByte* Pixels => _pixels;

        public ref DefaultImageSource? NextPooled => ref _nextPooled;

        private DefaultImageSource(int width, int height, bool zeroFill)
        {
            Init(width, height, zeroFill);
            _token = _tokenFactory.CreateNonZeroToken();
        }

        ~DefaultImageSource() => Dispose(false);

        private void Init(int width, int height, bool zeroFill)
        {
            _width = width;
            _height = height;
            var len = width * height;
            _pixels = (ColorByte*)Marshal.AllocHGlobal(sizeof(ColorByte) * len);
            if(zeroFill) {
                new Span<ColorByte>(_pixels, len).Clear();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Image CreateImage(int width, int height, bool zeroFill)
        {
            var source = CreateSource(width, height, zeroFill);
            return new Image(source, source.Token);
        }

        public static DefaultImageSource CreateSource(int width, int height, bool zeroFill)
        {
            if(ChainInstancePool<DefaultImageSource>.TryGetInstanceFast(out var source)) {
                source.Init(width, height, zeroFill);
            }
            else {
                source = new DefaultImageSource(width, height, zeroFill);
            }
            return source;
        }

        public Span<ColorByte> GetPixels() => new Span<ColorByte>(_pixels, _width * _height);

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            Marshal.FreeHGlobal((IntPtr)_pixels);
            _pixels = null;
            _width = 0;
            _height = 0;
            _token = 0;
            if(disposing) {
                _token = _tokenFactory.CreateNonZeroToken();
                ChainInstancePool<DefaultImageSource>.ReturnInstanceFast(this);
            }
        }

        public override string ToString() => DebugView;
    }
}
