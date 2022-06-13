#nullable enable
using Elffy.Components;
using Elffy.Components.Implementation;
using Elffy.Graphics.OpenGL;
using Elffy.Shading;
using Elffy.Imaging;
using System;
using SkiaSharp;

namespace Elffy.UI
{
    public sealed class ButtonDefaultShader : UIRenderingShader
    {
        private TextureCore _textureCore;
        private ResourceFile _typeface;
        private IHostScreen? _screen;
        private Button? _target;
        private EventUnsubscriber<(ITextContent Sender, string PropertyName)> _unsubscriber;
        private bool _requireUpdateTexture;

        protected override string VertexShaderSource => VertSource;

        protected override string FragmentShaderSource => FragSource;

        public ResourceFile Typeface
        {
            get => _typeface;
            set
            {
                _typeface = value;
            }
        }

        public ButtonDefaultShader()
        {
            _textureCore = new TextureCore(TextureConfig.DefaultNearestNeighbor);
            _requireUpdateTexture = true;
        }

        private unsafe void EraseAndDraw(Button target)
        {
            // TODO: font
            using var stream = Typeface.GetStream();
            using var tf = SKTypeface.FromStream(stream);
            using var font = new SKFont(tf);
            font.Size = target.FontSize;
            font.Subpixel = true;
            var options = new TextDrawOptions
            {
                Font = font,
                TargetSize = target.ActualSize,
                Background = target.Background.ToColorByte(),
                Alignment = target.TextAlignment,
                Foreground = target.Foreground,
            };
            using var result = TextDrawer.Draw(target.Text, options);
            if(result.IsNone) {
                return;
            }
            var textureSize = _textureCore.Size;
            var pos = result.Position;
            var image = result.Image;
            if((pos.X < 0) || (pos.X + image.Width > textureSize.X) ||
               (pos.Y < 0) || (pos.Y + image.Height > textureSize.Y)) {
                var destRowStart = Math.Max(pos.Y, 0);
                var destRowEnd = Math.Min(pos.Y + image.Height, textureSize.Y);
                var destColStart = Math.Min(Math.Max(0, pos.X), textureSize.X);
                var destColEnd = Math.Min(Math.Max(0, pos.X + image.Width), textureSize.X);
                var width = destColEnd - destColStart;
                var height = destRowEnd - destRowStart;
                var x = Math.Min(Math.Max(0, -pos.X), image.Width);
                var y = Math.Min(Math.Max(0, -pos.Y), image.Height);
                using var subimage = image.ToSubimage(x, y, width, height);
                _textureCore.Update(new Vector2i(destColStart, destRowStart), subimage);
            }
            else {
                _textureCore.Update(result.Position, result.Image);
            }
        }

        protected override void DefineLocation(VertexDefinition<VertexSlim> definition, Control target)
        {
            _screen = Engine.GetValidCurrentContext();
            definition.Map("_vPos", nameof(VertexSlim.Position));
            definition.Map("_vUV", nameof(VertexSlim.UV));

            _textureCore.Load((Vector2i)target.ActualSize, ColorByte.Transparent);

            if(target is Button button) {
                _target = button;
                _unsubscriber = button.TextContentChanged.Subscribe(_ => _requireUpdateTexture = true);   // capture this
            }
        }

        protected override void OnRendering(ShaderDataDispatcher dispatcher, Control target, in Matrix4 model, in Matrix4 view, in Matrix4 projection)
        {
            var screen = _screen;
            if(screen == null) { return; }
            if(_requireUpdateTexture && _target is not null) {
                EraseAndDraw(_target);
                _requireUpdateTexture = false;
            }

            var mvp = projection * view * model;
            dispatcher.SendUniform("_mvp", mvp);
            dispatcher.SendUniformTexture2D("_tex", _textureCore.Texture, TextureUnitNumber.Unit0);
            dispatcher.SendUniform("_background", target.Background);
            dispatcher.SendUniform("_screenHeight", screen.FrameBufferSize.Y);
            dispatcher.SendUniform("_origin", target.ActualPosition);

            dispatcher.SendUniform("_size", target.ActualSize);
            dispatcher.SendUniform("_cornerRadius", target.CornerRadius);
        }

        protected override void OnProgramDisposed()
        {
            base.OnProgramDisposed();
            _textureCore.Dispose();
            _unsubscriber.Dispose();
        }

        private const string VertSource =
@"#version 410
in vec3 _vPos;
in vec2 _vUV;
out vec2 _uv;
uniform mat4 _mvp;

void main()
{
    gl_Position = _mvp * vec4(_vPos, 1.0);
    _uv = _vUV;
}
";
        private const string FragSource =
@"#version 410
in vec2 _uv;
out vec4 _fragColor;
uniform sampler2D _tex;
uniform vec4 _background;
uniform int _screenHeight;
uniform vec2 _origin;
uniform vec2 _size;
uniform vec4 _cornerRadius;

float FilterCorner(vec4 r, vec2 uv, vec2 size)
{
    vec2 p = uv * size;
    vec2[4] center = vec2[]
    (
        vec2(r[0], r[0]),
        vec2(size.x - r[1], r[1]),
        vec2(size.x - r[2], size.y - r[2]),
        vec2(r[3], _size.y - r[3])
    );
    vec4 isCorner = vec4
    (
        (1.0 - step(center[0].x, p.x)) * (1.0 - step(center[0].y, p.y)),
        (1.0 - step(p.x, center[1].x)) * (1.0 - step(center[1].y, p.y)),
        (1.0 - step(p.x, center[2].x)) * (1.0 - step(p.y, center[2].y)),
        (1.0 - step(center[3].x, p.x)) * (1.0 - step(p.y, center[3].y))
    );
    vec4 l = vec4
    (
        length(p - center[0]),
        length(p - center[1]),
        length(p - center[2]),
        length(p - center[3])
    );
    vec4 a = clamp(-l + r + 0.5, 0.0, 1.0);
    return dot(a, isCorner) + step(isCorner[0] + isCorner[1] + isCorner[2] + isCorner[3], 0.5);
}

void main()
{
    vec2 p = vec2(gl_FragCoord.x, _screenHeight - gl_FragCoord.y) - _origin;
    vec4 color = texelFetch(_tex, ivec2(p), 0);
    _fragColor = vec4(mix(_background.rgb, color.rgb, color.a), _background.a * (1 - color.a) + color.a);
    _fragColor.a *= FilterCorner(_cornerRadius, _uv, _size);
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
