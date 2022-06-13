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
uniform mat4 _mvp;

void main()
{
    gl_Position = _mvp * vec4(_vPos, 1.0);
}
";
        private const string FragSource =
@"#version 410
out vec4 _fragColor;
uniform sampler2D _tex;
uniform vec4 _background;
uniform int _screenHeight;
uniform vec2 _origin;

void main()
{
    vec2 p = vec2(gl_FragCoord.x, _screenHeight - gl_FragCoord.y) - _origin;
    vec4 color = texelFetch(_tex, ivec2(p), 0);
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
