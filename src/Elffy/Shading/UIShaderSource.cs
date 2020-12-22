#nullable enable
using Elffy.Core;
using Elffy.Diagnostics;
using Elffy.OpenGL;
using Elffy.UI;

namespace Elffy.Shading
{
    [ShaderTargetVertexType(typeof(VertexSlim))]
    internal sealed class UIShaderSource : ShaderSource
    {
        protected override string VertexShaderSource => VertSource;

        protected override string FragmentShaderSource => FragSource;

        public static readonly UIShaderSource Instance = new UIShaderSource();

        private UIShaderSource()
        {
        }

        protected override void DefineLocation(VertexDefinition definition, Renderable target)
        {
            definition.Map<VertexSlim>("vPos", nameof(VertexSlim.Position));
            definition.Map<VertexSlim>("vUV", nameof(VertexSlim.UV));
        }

        protected override void SendUniforms(Uniform uniform, Renderable target, in Matrix4 model, in Matrix4 view, in Matrix4 projection)
        {
            var uiRenderable = SafeCast.As<UIRenderable>(target);
            var mvp = projection * view * model;
            uniform.Send("mvp", mvp);

            var texture = uiRenderable.Control.Texture;
            uniform.SendTexture2D("tex_sampler", texture.TextureObject, TextureUnitNumber.Unit0);
            uniform.Send("hasTexture", !texture.IsEmpty);

            var control = uiRenderable.Control;
            var uvScale = new Vector2(control.Width, control.Height) / texture.Size;
            uniform.Send("uvScale", uvScale);
        }

        private const string VertSource =
@"#version 440

in vec3 vPos;
in vec2 vUV;
out vec2 UV;

uniform vec2 uvScale;
uniform mat4 mvp;

void main()
{
    UV = vUV * uvScale;
    gl_Position = mvp * vec4(vPos, 1.0);
}
";

        private const string FragSource =
@"#version 440

in vec2 UV;
out vec4 fragColor;
uniform sampler2D tex_sampler;
uniform bool hasTexture;

void main()
{
    fragColor = hasTexture ? texture(tex_sampler, UV) : vec4(1.0, 1.0, 1.0, 1.0);
}
";
    }
}
