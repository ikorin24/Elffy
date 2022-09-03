#nullable enable
using Elffy.Components;
using Elffy.Components.Implementation;
using Elffy.Graphics.OpenGL;
using Elffy.Shading;
using Elffy.Imaging;
using System;

namespace Elffy.UI
{
    internal abstract class ControlDefaultShaderBase : UIRenderingShader
    {
        private TextureCore _textureCore;

        protected ControlDefaultShaderBase()
        {
            _textureCore = new TextureCore(TextureConfig.DefaultNearestNeighbor);
        }

        protected override void DefineLocation(VertexDefinition definition, Control target, Type vertexType)
        {
            definition.Map(vertexType, "_vPos", VertexSpecialField.Position);
            definition.Map(vertexType, "_vUV", VertexSpecialField.UV);
        }

        protected override void OnRendering(ShaderDataDispatcher dispatcher, Control target, in Matrix4 model, in Matrix4 view, in Matrix4 projection)
        {
            var background = GetBackground(target);
            dispatcher.SendUniform("_background", background);
            var mvp = projection * view * model;
            dispatcher.SendUniform("_mvp", mvp);

            var texture = _textureCore.Texture;
            var hasTex = !texture.IsEmpty;
            dispatcher.SendUniform("_hasTex", hasTex);
            if(hasTex) {
                var screen = target.GetValidScreen();
                dispatcher.SendUniformTexture2D("_tex", texture, TextureUnitNumber.Unit0);
                dispatcher.SendUniform("_screenHeight", screen.FrameBufferSize.Y);
                dispatcher.SendUniform("_origin", target.ActualPosition);

                var imageSize = _textureCore.Size;
                var (h, v) = GetImageAlignment(target);
                var imagePos = CalcImagePosition(target.ActualSize, imageSize, h, v);
                dispatcher.SendUniform("_imagePos", imagePos);
                dispatcher.SendUniform("_imageSize", imageSize);
            }

            var cornerRadius = target.ActualCornerRadius;
            var hasCornerRadius = !cornerRadius.IsZero;
            dispatcher.SendUniform("_hasCornerRadius", hasCornerRadius);
            if(hasCornerRadius) {
                dispatcher.SendUniform("_size", target.ActualSize);
                dispatcher.SendUniform("_cornerRadius", cornerRadius);
            }
        }

        static Vector2 CalcImagePosition(Vector2 targetSize, Vector2i imageSize, HorizontalAlignment hAlignment, VerticalAlignment vAlignment)
        {
            var pos = new Vector2
            {
                X = hAlignment switch
                {
                    HorizontalAlignment.Center => (targetSize.X - imageSize.X) / 2,
                    HorizontalAlignment.Right => targetSize.X - imageSize.X,
                    HorizontalAlignment.Left or _ => 0,
                },
                Y = vAlignment switch
                {
                    VerticalAlignment.Center => (targetSize.Y - imageSize.Y) / 2,
                    VerticalAlignment.Bottom => targetSize.Y - imageSize.Y,
                    VerticalAlignment.Top or _ => 0,
                }
            };
            return pos;
        }

        protected virtual Color4 GetBackground(Control target) => target.Background;

        protected virtual (HorizontalAlignment HAlignment, VerticalAlignment VAlignment) GetImageAlignment(Control target)
        {
            return (HorizontalAlignment.Center, VerticalAlignment.Center);
        }

        protected void LoadImage(in ReadOnlyImageRef image) => _textureCore.Load(image);

        protected void LoadImage(in Vector2i size, ColorByte fill) => _textureCore.Load(size, fill);

        protected void UpdateImage(Vector2i position, in ReadOnlyImageRef image)
        {
            if(image.IsEmpty) {
                return;
            }
            var textureSize = _textureCore.Size;
            if((position.X < 0) || (position.X + image.Width > textureSize.X) ||
               (position.Y < 0) || (position.Y + image.Height > textureSize.Y)) {
                var destRowStart = Math.Max(position.Y, 0);
                var destRowEnd = Math.Min(position.Y + image.Height, textureSize.Y);
                var destColStart = Math.Min(Math.Max(0, position.X), textureSize.X);
                var destColEnd = Math.Min(Math.Max(0, position.X + image.Width), textureSize.X);
                var width = destColEnd - destColStart;
                var height = destRowEnd - destRowStart;
                var x = Math.Min(Math.Max(0, -position.X), image.Width);
                var y = Math.Min(Math.Max(0, -position.Y), image.Height);
                using var subimage = image.ToSubimage(x, y, width, height);
                _textureCore.Update(new Vector2i(destColStart, destRowStart), subimage);
            }
            else {
                _textureCore.Update(position, image);
            }
        }

        protected void ReleaseImage() => _textureCore.Dispose();

        protected override void OnProgramDisposed()
        {
            base.OnProgramDisposed();
            _textureCore.Dispose();
        }

        protected override ShaderSource GetShaderSource(Renderable target, WorldLayer layer)
        {
            return new()
            {
                VertexShader = VertSource,
                FragmentShader = FragSource,
            };
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
#define LE(th,x) (step(th,x))   // (th <= x)
#define LT(th,x) (1-step(x,th)) // (th < x)
#define GE(th,x) (step(x,th))   // (th >= x)
#define GT(th,x) (1-step(th,x)) // (th > x)

in vec2 _uv;
out vec4 _fragColor;

uniform vec4 _background;

uniform bool _hasTex;
uniform sampler2D _tex;
uniform int _screenHeight;
uniform vec2 _origin;
uniform vec2 _imagePos;
uniform ivec2 _imageSize;

uniform bool _hasCornerRadius;
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
    if(_hasTex) {
        vec2 v = vec2(gl_FragCoord.x, _screenHeight - gl_FragCoord.y);
        ivec2 p = ivec2(v - _origin - _imagePos);
        vec4 color = LE(0, p.x) * LT(p.x, _imageSize.x) * LE(0, p.y) * LT(p.y, _imageSize.y) * texelFetch(_tex, p, 0);
        _fragColor = vec4(mix(_background.rgb, color.rgb, color.a), _background.a * (1 - color.a) + color.a);
    }
    else {
        _fragColor = _background;
    }

    if(_hasCornerRadius) {
        _fragColor.a *= FilterCorner(_cornerRadius, _uv, _size);
    }
}
";
    }
}
