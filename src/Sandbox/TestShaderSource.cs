#nullable enable
using Elffy;
using Elffy.Core;
using Elffy.Shading;
using Elffy.Diagnostics;
using System;
using Elffy.Components;

namespace Sandbox
{
    [ShaderTargetVertexType(typeof(RigVertex))]
    public sealed class TestShaderSource : ShaderSource
    {
        public static readonly TestShaderSource Instance = new TestShaderSource();

        protected override string VertexShaderSource => VertexShader;

        protected override string FragmentShaderSource => FragmentShader;

        private TestShaderSource()
        {
        }

        protected override void DefineLocation(VertexDefinition definition, Renderable target)
        {
            definition.Map<RigVertex>(nameof(RigVertex.Position), "_pos");
            definition.Map<RigVertex>(nameof(RigVertex.Bone), "_bone");
        }

        protected override void SendUniforms(Uniform uniform, Renderable target, ReadOnlySpan<Light> lights, in Matrix4 model, in Matrix4 view, in Matrix4 projection)
        {
            uniform.Send("_mvp", projection * view * model);
            var data = target.GetComponent<FloatDataTexture>();
            data.Apply(Elffy.OpenGL.TextureUnitNumber.Unit1);
            uniform.Send("_data", Elffy.OpenGL.TextureUnitNumber.Unit1);
        }

        private const string VertexShader =
@"#version 440
in vec3 _pos;
in ivec4 _bone;

uniform mat4 _mvp;

void main()
{
    gl_Position = _mvp * vec4(_pos, 1.0);
}
";

        private const string FragmentShader =
@"#version 440
out vec4 fragColor;

uniform sampler1D _data;

vec4 get_data(int index)
{
    return texelFetch(_data, index, 0);
}

void main()
{
    fragColor = vec4(0.25, 0.0, 0.0, 1.0) * get_data(0) + (vec4(0.0, 5.5, 5.5, 0.0) + get_data(1));
}
";
    }
}
