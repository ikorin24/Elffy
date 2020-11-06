#nullable enable
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Elffy.Core;
using Elffy.Effective;
using Elffy.Exceptions;
using Elffy.Imaging;
using Elffy.OpenGL;
using OpenTK.Graphics.OpenGL4;
using SkiaSharp;
using PixelFormat = System.Drawing.Imaging.PixelFormat;
using TKPixelFormat = OpenTK.Graphics.OpenGL4.PixelFormat;

namespace Elffy.Components
{
    public sealed partial class Texture : ISingleOwnerComponent, IDisposable
    {
        // I use GL_RGBA as internal pixel format (format in GPU).
        //
        // | LSB  <---               --->  MAB |
        // |   R    |   G    |   B    |   A    |
        // | 1 byte | 1 byte | 1 byte | 1 byte |
        // |             4 bytes               |
        //
        // The driver of opengl convert it if we send pixels as other formats.
        // Avoid that if possible.
        private PixelInternalFormat InternalFormat = PixelInternalFormat.Rgba;


        private SingleOwnerComponentCore<Texture> _core;    // Mutable object, Don't change into reaadonly
        private TextureObject _to;
        private Vector2i _size;
        public TextureExpansionMode ExpansionMode { get; }
        public TextureShrinkMode ShrinkMode { get; }
        public TextureMipmapMode MipmapMode { get; }

        public TextureObject TextureObject => _to;

        public ComponentOwner? Owner => _core.Owner;

        public bool IsEmpty => _to.IsEmpty;

        public bool AutoDisposeOnDetached => _core.AutoDisposeOnDetached;

        public ref readonly Vector2i Size => ref _size;

        public Texture(TextureExpansionMode expansionMode, TextureShrinkMode shrinkMode, TextureMipmapMode mipmapMode, bool autoDispose = true)
        {
            ExpansionMode = expansionMode;
            ShrinkMode = shrinkMode;
            MipmapMode = mipmapMode;
            _core = new SingleOwnerComponentCore<Texture>(autoDispose);
        }

        ~Texture() => Dispose(false);

        public void Load(Bitmap bitmap)
        {
            if(!_to.IsEmpty) { throw new InvalidOperationException("Texture is already loaded."); }
            if(bitmap is null) { throw new ArgumentNullException(nameof(bitmap)); }

            using(var pixels = bitmap.GetPixels(ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb)) {
                LoadPrivate(new Vector2i(pixels.Width, pixels.Height), pixels.Ptr, TKPixelFormat.Bgra);
            }
        }

        public unsafe void Load(in Vector2i size, in ColorByte fill)
        {
            if(!_to.IsEmpty) { throw new InvalidOperationException("Texture is already loaded."); }

            using(var buf = new PooledArray<byte>(size.X * size.Y * sizeof(ColorByte))) {
                var pixels = buf.AsSpan().MarshalCast<byte, ColorByte>();
                pixels.Fill(fill);
                fixed(void* ptr = pixels) {
                    LoadPrivate(size, (IntPtr)ptr, TKPixelFormat.Rgba);
                }
            }
        }

        internal unsafe void LoadUndefinedEmpty(in Vector2i size)
        {
            if(!_to.IsEmpty) {
                ThrowAlreadyLoaded();
                static void ThrowAlreadyLoaded() => throw new InvalidOperationException("Texture is already loaded.");
            }
            LoadPrivate(size, IntPtr.Zero, TKPixelFormat.Rgba);
        }

        public unsafe int GetPixels(in RectI rect, Span<ColorByte> buffer)
        {
            if(_to.IsEmpty) {
                return 0;
            }
            if((uint)rect.X >= (uint)_size.X ||
               (uint)rect.Y >= (uint)_size.Y ||
               (uint)rect.Width > (uint)(_size.X - rect.X) ||
               (uint)rect.Height > (uint)(_size.Y - rect.Y)) {
                throw new ArgumentOutOfRangeException();
            }
            var len = rect.X * rect.Y * sizeof(ColorByte);
            if(buffer.Length < len) {
                throw new ArgumentException($"{nameof(buffer)} is too short.");
            }

            // Get current binded fbo
            var currentRead = FBO.CurrentReadBinded;
            var currentDraw = FBO.CurrentDrawBinded;

            var fbo = FBO.Create();
            try {
                FBO.Bind(fbo, FBO.Target.FrameBuffer);
                FBO.SetTexture2DBuffer(_to, FBO.Attachment.ColorAttachment0);
                if(!FBO.CheckStatus(out var status)) {
                    throw new Exception(status.ToString());
                }
                fixed(void* ptr = buffer) {
                    GL.ReadPixels(rect.X, rect.Y, rect.Width, rect.Height, TKPixelFormat.Rgba, PixelType.UnsignedByte, (IntPtr)ptr);
                }
            }
            finally {
                FBO.Unbind(FBO.Target.FrameBuffer);
                FBO.Delete(ref fbo);

                // Restore binded
                FBO.Bind(currentRead, FBO.Target.Read);
                FBO.Bind(currentDraw, FBO.Target.Draw);
            }
            return len;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Painter GetPainter(bool copyFromOriginal = true)
        {
            if(_to.IsEmpty) {
                ThrowEmptyTexture();
                static void ThrowEmptyTexture() => throw new InvalidOperationException("Texture is not loaded yet.");
            }

            return new Painter(this, new RectI(0, 0, _size.X, _size.Y), copyFromOriginal);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Painter GetPainter(in RectI rect, bool copyFromOriginal = true)
        {
            if(_to.IsEmpty) {
                ThrowEmptyTexture();
                static void ThrowEmptyTexture() => throw new InvalidOperationException("Texture is not loaded yet.");
            }

            if((uint)rect.X >= (uint)_size.X) { ThrowOutOfRange(); }
            if((uint)rect.Y >= (uint)_size.Y) { ThrowOutOfRange(); }
            if((uint)rect.Width > (uint)(_size.X - rect.X)) { ThrowOutOfRange(); }
            if((uint)rect.Height > (uint)(_size.Y - rect.Y)) { ThrowOutOfRange(); }

            return new Painter(this, rect, copyFromOriginal);

            static void ThrowOutOfRange() => throw new ArgumentOutOfRangeException();
        }


        private void LoadPrivate(in Vector2i size, IntPtr ptr, TKPixelFormat format)
        {
            _to = TextureObject.Create();
            const TextureUnitNumber unit = TextureUnitNumber.Unit0;
            TextureObject.Bind2D(_to, unit);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, GetMinParameter(ShrinkMode, MipmapMode));
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, GetMagParameter(ExpansionMode));
            GL.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat, size.X, size.Y, 0, format, PixelType.UnsignedByte, ptr);
            if(MipmapMode != TextureMipmapMode.None) {
                GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
            }
            TextureObject.Unbind2D(unit);
            _size = size;
        }

        private unsafe void UpdateSubTexture(byte* pixels, in RectI rect)
        {
            // This method is called from Painter

            Debug.Assert(_to.IsEmpty == false);
            const TextureUnitNumber unit = TextureUnitNumber.Unit0;
            TextureObject.Bind2D(_to, unit);
            GL.TexSubImage2D(TextureTarget.Texture2D, 0, rect.X, rect.Y, rect.Width, rect.Height, TKPixelFormat.Rgba, PixelType.UnsignedByte, (IntPtr)pixels);
            TextureObject.Unbind2D(unit);
        }

        //private 

        void IComponent.OnAttached(ComponentOwner owner) => _core.OnAttached(owner);

        void IComponent.OnDetached(ComponentOwner owner) => _core.OnDetachedForDisposable(owner, this);

        public void Dispose()
        {
            if(_to.IsEmpty) { return; }
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if(_to.IsEmpty) { return; }
            if(disposing) {
                TextureObject.Delete(ref _to);
                _size = default;
            }
            else {
                // Cannot release objects of OpenGL from the finalizer thread.
                throw new MemoryLeakException(typeof(Texture));
            }
        }


        private static int GetMagParameter(TextureExpansionMode expansionMode)
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

        private static int GetMinParameter(TextureShrinkMode shrinkMode, TextureMipmapMode mipmapMode)
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

        public unsafe partial struct Painter : IDisposable
        {
            // This color type is same as texture inner pixel format of opengl
            const SKColorType ColorType = SKColorType.Rgba8888;

            private readonly RectI _rect;
            private readonly Texture _t;
            private SKPaint? _paint;
            private SKBitmap? _bitmap;
            private SKCanvas? _canvas;
            private SKTextBlobBuilder? _textBuilder;
            private bool _isDirty;
            private byte* _pixels;      // pointer to a head pixel

            private SKPaint Paint => _paint ??= new SKPaint();
            private SKBitmap Bitmap
            {
                get
                {
                    if(_bitmap is null) {
                        _bitmap = new SKBitmap(new SKImageInfo(_rect.Width, _rect.Height, ColorType, SKAlphaType.Unpremul));
                    }
                    return _bitmap;
                }
            }
            private SKCanvas Canvas => _canvas ??= new SKCanvas(Bitmap);
            private SKTextBlobBuilder TextBuilder => _textBuilder ??= new SKTextBlobBuilder();

            internal Painter(Texture texture, in RectI rect, bool copyFromOriginal)
            {
                _rect = rect;
                _t = texture;
                _isDirty = false;
                _pixels = default;
                _paint = null;
                _bitmap = null;
                _canvas = null;
                _textBuilder = null;
                if(copyFromOriginal) {
                    CopyFromOriginal(rect);
                }
            }

            public void Flush()
            {
                if(_isDirty) {
                    Debug.Assert(_pixels != null);
                    _t.UpdateSubTexture(_pixels, _rect);
                    _isDirty = false;
                }
            }

            public void Dispose()
            {
                Flush();
                _paint?.Dispose();
                _bitmap?.Dispose();
                _canvas?.Dispose();
                _textBuilder?.Dispose();
            }

            private void CopyFromOriginal(in RectI rect)
            {
                var texture = _t;
                var pixCount = texture.Size.X * texture.Size.Y;
                var buf = new Span<ColorByte>(Bitmap.GetPixels().ToPointer(), pixCount);
                texture.GetPixels(rect, buf);
            }
        }
    }
}
