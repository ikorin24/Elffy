#nullable enable
using Elffy.Core;
using Elffy.Diagnostics;
using Elffy.OpenGL;
using Elffy.UI;
using System;

namespace Elffy.Shading
{
    [ShaderTargetVertexType(typeof(VertexSlim))]
    internal sealed class DefaultUIShaderSource : UIShaderSource
    {
        public override string VertexShaderSource => VertSource;

        public override string FragmentShaderSource => FragSource;

        public static readonly DefaultUIShaderSource Instance = new DefaultUIShaderSource();

        private DefaultUIShaderSource()
        {
        }

        protected override void DefineLocation(VertexDefinition<VertexSlim> definition, Control target)
        {
            definition.Map("vPos", nameof(VertexSlim.Position));
            definition.Map("vUV", nameof(VertexSlim.UV));
        }

        protected override void SendUniforms(Uniform uniform, Control target, in Matrix4 model, in Matrix4 view, in Matrix4 projection)
        {
            var mvp = projection * view * model;
            uniform.Send("mvp", mvp);

            ref var fixedArea = ref target.TextureFixedArea;

            ref readonly var texture = ref target.Texture;
            var hasTexture = texture.IsEmpty == false;
            uniform.SendTexture2D("tex_sampler", texture, TextureUnitNumber.Unit0);
            uniform.Send("hasTexture", hasTexture);

            if(hasTexture) {
                var controlSize = (Vector2)target.ActualSize;
                var a = new Vector2(fixedArea.Left, fixedArea.Bottom) / controlSize;
                var b = (controlSize - new Vector2(fixedArea.Right, fixedArea.Top)) / controlSize;
                var uvScale = controlSize / target.ActualTextureSize;
                uniform.Send("a", a);
                uniform.Send("b", b);
                uniform.Send("uvScale", uvScale);
            }
            uniform.Send("back", target.Background);
        }

        private const string VertSource =
@"#version 410

in vec3 vPos;
in vec2 vUV;
out vec2 UV;
uniform mat4 mvp;

void main()
{
    UV = vUV;
    gl_Position = mvp * vec4(vPos, 1.0);
}
";

        private const string FragSource =
@"#version 410

in vec2 UV;
out vec4 fragColor;
uniform vec2 uvScale;
uniform vec2 a;
uniform vec2 b;
uniform sampler2D tex_sampler;
uniform bool hasTexture;
uniform vec4 back;

void main()
{
    if(hasTexture) {
        float[3] xValues = float[](
            UV.x * uvScale.x,
            a.x * uvScale.x + UV.x - a.x,
            b.x + (UV.x - b.x) * uvScale.x
        );
        float[3] yValues = float[](
            UV.y * uvScale.y,
            a.y * uvScale.y + UV.y - a.y,
            b.y + (UV.y - b.y) * uvScale.y
        );
        vec2 uv = vec2(xValues[int(step(a.x, UV.x) + step(b.x, UV.x))],
                       yValues[int(step(a.y, UV.y) + step(b.y, UV.y))]);
        float isOutOfRange = step(0.5, (1.0 - step(0, uv.x)) + step(1, uv.x) + (1.0 - step(0, uv.y)) + step(1, uv.y));

        vec4 t = texture(tex_sampler, uv) * (1.0 - isOutOfRange);
        
        float tai = 1.0 - t.a;
        fragColor = vec4(t.r * t.a + back.r * tai,
                         t.g * t.a + back.g * tai,
                         t.b * t.a + back.b * tai,
                         t.a + back.a * tai);
    }
    else {
        fragColor = back;
    }
}
";
    }
}
