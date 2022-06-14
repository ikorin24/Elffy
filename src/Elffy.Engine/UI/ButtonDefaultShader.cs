#nullable enable
using Elffy.Components;
using Elffy.Components.Implementation;
using Elffy.Graphics.OpenGL;
using Elffy.Shading;
using Elffy.Imaging;
using System;
using SkiaSharp;
using System.Diagnostics;

namespace Elffy.UI
{
    internal sealed class ButtonDefaultShader : UIRenderingShader
    {
        private TextureCore _textureCore;
        private IHostScreen? _screen;
        private ITextContent? _target;
        private EventUnsubscriber<(ITextContent Sender, string PropertyName)> _unsubscriber;
        private bool _requireUpdateTexture;

        protected override string VertexShaderSource => VertSource;

        protected override string FragmentShaderSource => FragSource;

        public ButtonDefaultShader()
        {
            _textureCore = new TextureCore(TextureConfig.DefaultNearestNeighbor);
            _requireUpdateTexture = true;
        }

        private unsafe void EraseAndDraw(ITextContent target, Vector2 targetSize)
        {
            // TODO: font
            var fontFamily = FontFamilies.Instance.GetFontFamilyOrDefault(target.FontFamily);
            using var font = fontFamily.SkTypeface != null ? new SKFont(fontFamily.SkTypeface) : new SKFont();
            font.Size = target.FontSize;
            font.Subpixel = true;
            var options = new TextDrawOptions
            {
                Font = font,
                TargetSize = targetSize,
                Background = ColorByte.Transparent,
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

            if(target is ITextContent textContent) {
                _target = textContent;
                _unsubscriber = textContent.TextContentChanged.Subscribe(x =>
                {
                    // avoid capturing 'this'
                    var shader = SafeCast.As<Control>(x.Sender).Shader;
                    Debug.Assert(shader is not null);
#if DEBUG
                    Debug.Assert(ReferenceEquals(shader, this));
#endif
                    var self = SafeCast.As<ButtonDefaultShader>(shader);
                    self._requireUpdateTexture = true;
                });
            }
        }

        protected override void OnRendering(ShaderDataDispatcher dispatcher, Control target, in Matrix4 model, in Matrix4 view, in Matrix4 projection)
        {
            if(ReferenceEquals(target, _target) == false) {
                Debug.Fail("invalid target");
                return;
            }
            var screen = _screen;
            if(screen == null) {
                Debug.Fail("Why is the screen null ?");
                return;
            }
            if(_requireUpdateTexture) {
                EraseAndDraw(_target, target.ActualSize);
                _requireUpdateTexture = false;
            }
            var background = (target is Executable executable) ? GetBackground(executable) : target.Background;

            var mvp = projection * view * model;
            dispatcher.SendUniform("_mvp", mvp);
            dispatcher.SendUniformTexture2D("_tex", _textureCore.Texture, TextureUnitNumber.Unit0);
            dispatcher.SendUniform("_background", background);
            dispatcher.SendUniform("_screenHeight", screen.FrameBufferSize.Y);
            dispatcher.SendUniform("_origin", target.ActualPosition);

            dispatcher.SendUniform("_size", target.ActualSize);
            dispatcher.SendUniform("_cornerRadius", target.CornerRadius);
        }

        private static Color4 GetBackground(Executable control)
        {
            return control.IsKeyPressed ? Color4.Red :
                   control.IsMouseOver ? Color4.BlueViolet :
                   control.Background;
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
