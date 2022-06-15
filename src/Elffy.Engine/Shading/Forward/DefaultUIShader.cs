#nullable enable
using Elffy.UI;

namespace Elffy.Shading.Forward
{
    internal sealed class DefaultUIShader : UIRenderingShader
    {
        protected override string VertexShaderSource => VertSource;

        protected override string FragmentShaderSource => FragSource;

        public static readonly DefaultUIShader Instance = new DefaultUIShader();

        private DefaultUIShader()
        {
        }

        protected override void DefineLocation(VertexDefinition<VertexSlim> definition, Control target)
        {
            definition.Map("vPos", nameof(VertexSlim.Position));
        }

        protected override void OnRendering(ShaderDataDispatcher dispatcher, Control target, in Matrix4 model, in Matrix4 view, in Matrix4 projection)
        {
            var mvp = projection * view * model;
            dispatcher.SendUniform("_mvp", mvp);
            dispatcher.SendUniform("_background", target.Background);
        }

        private const string VertSource =
@"#version 410
in vec3 vPos;
uniform mat4 _mvp;
void main()
{
    gl_Position = _mvp * vec4(vPos, 1.0);
}
";

        private const string FragSource =
@"#version 410
out vec4 fragColor;
uniform vec4 _background;
void main()
{
    fragColor = _background;
}
";
    }
}

//    vec4 t = textureLod(tex_sampler, UV, 0);
//    float tai = 1.0 - t.a;
//    fragColor = vec4(t.r * t.a + back.r * tai,
//                     t.g * t.a + back.g * tai,
//                     t.b * t.a + back.b * tai,
//                     t.a + back.a * tai);
