#nullable enable
using Elffy.Components;
using Elffy.Components.Implementation;
using Elffy.Graphics.OpenGL;
using Elffy.Shading;
using Elffy.Imaging;
using Elffy.Effective;
using System;
using SkiaSharp;

namespace Elffy.UI
{
    public sealed class TextUIShader : UIRenderingShader
    {
        private TextureCore _textureCore;
        private string? _text;
        private ResourceFile _typeface;
        private IHostScreen? _screen;
        private int _fontSize;
        private ColorByte _textColor = new ColorByte(0, 0, 0, 255);
        private ColorByte _background = new ColorByte(230, 230, 230, 255);
        private HorizontalTextAlignment _textAlignment;
        private bool _isDirty;

        protected override string VertexShaderSource => VertSource;

        protected override string FragmentShaderSource => FragSource;

        public string Text
        {
            get => _text;
            set
            {
                if(_text == value) { return; }
                _text = value;
                _isDirty = true;
            }
        }

        public HorizontalTextAlignment TextAlignment
        {
            get => _textAlignment;
            set
            {
                if(_textAlignment == value) { return; }
                _textAlignment = value;
                _isDirty = true;
            }
        }

        public ResourceFile Typeface
        {
            get => _typeface;
            set
            {
                _typeface = value;
                _isDirty = true;
            }
        }

        public int FontSize
        {
            get => _fontSize;
            set
            {
                _fontSize = value;
                _isDirty = true;
            }
        }

        public ColorByte TextColor
        {
            get => _textColor;
            set
            {
                if(_textColor == value) { return; }
                _textColor = value;
                _isDirty = true;
            }
        }

        public TextUIShader()
        {
            _textureCore = new TextureCore(TextureConfig.DefaultNearestNeighbor);
        }

        private unsafe void EraseAndDraw(Control target)
        {
            // TODO: font
            using var stream = Typeface.GetStream();
            using var tf = SKTypeface.FromStream(stream);
            using var font = new SKFont(tf);
            font.Size = 82;
            font.Subpixel = true;
            var textUtf16Bytes = _text.AsSpan().MarshalCast<char, byte>();
            DrawCore(textUtf16Bytes, SKTextEncoding.Utf16, font, target.ActualSize, _textAlignment, VerticalTextAlignment.Center, _textColor, _background, this, &Callback);
            return;

            static void Callback(ReadOnlyImageRef result, Vector2i pos, TextUIShader self)
            {
                var textureSize = self._textureCore.Size;

                // TODO:
                using var buf = new ValueTypeRentMemory<ColorByte>(textureSize.X * textureSize.Y, false);
                var dest = new ImageRef(buf.AsSpan(), textureSize.X, textureSize.Y);
                dest.GetPixels().Fill(ColorByte.Gray);
                result.CopyTo(dest, pos);
                self._textureCore.Update(Vector2i.Zero, dest);

                //self._textureCore.Update(pos, result);
            }
        }

        private static unsafe void DrawCore<TState>(
            ReadOnlySpan<byte> text,
            SKTextEncoding enc,
            SKFont skFont,
            in Vector2 targetSize,
            HorizontalTextAlignment hAlign,
            VerticalTextAlignment vAlign,
            ColorByte fontColor,
            ColorByte backgroundColor,
            TState state,
            delegate*<ReadOnlyImageRef, Vector2i, TState, void> callback)
        {
            var glyphCount = skFont.CountGlyphs(text, enc);
            if(glyphCount == 0) {
                return;
            }

            using var builder = new SKTextBlobBuilder();    // TODO: pool SKTextBlobBuilder instance
            using var paint = new SKPaint();  // TODO: pool
            paint.Reset();
            paint.Color = new SKColor(fontColor.R, fontColor.G, fontColor.B, fontColor.A);
            paint.Style = SKPaintStyle.Fill;
            paint.IsAntialias = true;
            paint.SubpixelText = true;
            paint.LcdRenderText = true;
            paint.TextAlign = SKTextAlign.Left;

            var buffer = builder.AllocatePositionedRun(skFont, glyphCount);
            var glyphs = buffer.GetGlyphSpan();
            skFont.GetGlyphs(text, enc, glyphs);
            var glyphPositions = buffer.GetPositionSpan();
            skFont.GetGlyphPositions(glyphs, glyphPositions);
            skFont.MeasureText(glyphs, out var bounds, paint);
            skFont.GetFontMetrics(out var metrics);

            var pos = new Vector2
            {
                X = hAlign switch
                {
                    HorizontalTextAlignment.Center => (int)((targetSize.X - bounds.Width) / 2),
                    HorizontalTextAlignment.Right => targetSize.X - bounds.Width,
                    HorizontalTextAlignment.Left or _ => 0,
                },
                Y = vAlign switch
                {
                    VerticalTextAlignment.Center => targetSize.Y / 2 - metrics.Bottom + (metrics.Bottom - metrics.Top) / 2,
                    VerticalTextAlignment.Top => -metrics.Top,
                    VerticalTextAlignment.Bottom => targetSize.Y - metrics.Bottom,
                    _ => 0,
                }
            };
            float y0 = pos.Y + metrics.Top;

            var posPixel = new Vector2i
            {
                X = (int)MathF.Floor(pos.X),
                Y = (int)MathF.Floor(y0),
            };
            var posSubpixel = new Vector2
            {
                X = pos.X - posPixel.X,
                Y = y0 - posPixel.Y,
            };

            var size = new Vector2i
            {
                X = (int)MathF.Ceiling(posSubpixel.X + bounds.Width),
                Y = (int)MathF.Ceiling(posSubpixel.Y + metrics.Bottom - metrics.Top),
            };

            // This color type is same as texture inner pixel format of opengl, and Elffy.ColorByte
            const SKColorType ColorType = SKColorType.Rgba8888;
            var info = new SKImageInfo(size.X, size.Y, ColorType, SKAlphaType.Premul);
            using var bitmap = new SKBitmap(info);
            using var canvas = new SKCanvas(bitmap);

            var pixelSpan = new Span<ColorByte>((ColorByte*)bitmap.GetPixels(), size.X * size.Y);
            pixelSpan.Fill(backgroundColor);

            using(var textBlob = builder.Build()) {
                var x = posSubpixel.X;
                var y = posSubpixel.Y - metrics.Top;
                canvas.DrawText(textBlob, x, y, paint);
            }

            var result = new ReadOnlyImageRef((ColorByte*)bitmap.GetPixels(), size.X, size.Y);
            callback(result, posPixel, state);
        }

        protected override void DefineLocation(VertexDefinition<VertexSlim> definition, Control target)
        {
            _screen = Engine.GetValidCurrentContext();
            definition.Map("_vPos", nameof(VertexSlim.Position));

            var size = target.ActualSize;
            _textureCore.Load((Vector2i)size, ColorByte.Transparent);
        }

        protected override void OnRendering(ShaderDataDispatcher dispatcher, Control target, in Matrix4 model, in Matrix4 view, in Matrix4 projection)
        {
            var screen = _screen;
            if(screen == null) { return; }
            if(_isDirty) {
                EraseAndDraw(target);
                _isDirty = false;
            }

            var mvp = projection * view * model;
            dispatcher.SendUniform("_mvp", mvp);
            dispatcher.SendUniformTexture2D("tex_sampler", _textureCore.Texture, TextureUnitNumber.Unit0);
            dispatcher.SendUniform("_background", target.Background);
            dispatcher.SendUniform("_screenHeight", screen.FrameBufferSize.Y);
            dispatcher.SendUniform("_origin", target.ActualPosition);
        }

        protected override void OnProgramDisposed()
        {
            base.OnProgramDisposed();
            _textureCore.Dispose();
        }

        private const string VertSource =
@"#version 410
in vec3 _vPos;
uniform mat4 _mvp;

void main()
{
    gl_Position = _mvp * vec4(_vPos, 1.0);
}
";
        private const string FragSource =
@"#version 410
out vec4 _fragColor;
uniform sampler2D tex_sampler;
uniform vec4 _background;
uniform int _screenHeight;
uniform vec2 _origin;

void main()
{
    vec2 p = vec2(gl_FragCoord.x, _screenHeight - gl_FragCoord.y) - _origin;
    vec4 color = texelFetch(tex_sampler, ivec2(p), 0);
    _fragColor = vec4(mix(_background.rgb, color.rgb, color.a), _background.a * (1 - color.a) + color.a);
}
";
    }

    public enum HorizontalTextAlignment : byte
    {
        Center = 0,
        Left,
        Right,
        //Justify,
    }

    public enum VerticalTextAlignment : byte
    {
        Center = 0,
        Top,
        Bottom,
    }
}
