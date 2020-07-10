#nullable enable
using Elffy;
using Elffy.Core;
using Elffy.Diagnostics;
using Elffy.Shading;
using System;

namespace Sandbox
{
    [ShaderTargetVertexType(typeof(Vertex))]
    public class SkyShaderSource : ShaderSource
    {
        public static readonly SkyShaderSource Instance = new SkyShaderSource();

        protected override string VertexShaderSource => VertSource;

        protected override string FragmentShaderSource => FragSource;

        private SkyShaderSource()
        {
        }

        protected override void DefineLocation(VertexDefinition definition)
        {
            definition.Map<Vertex>(nameof(Vertex.Position), "vPos");
            definition.Map<Vertex>(nameof(Vertex.TexCoord), "vUV");
        }

        protected override void SendUniforms(Uniform uniform, Renderable target, ReadOnlySpan<Light> lights, in Matrix4 model, in Matrix4 view, in Matrix4 projection)
        {
            uniform.Send("mvp", projection * view * model);
        }


        private const string VertSource =
@"#version 440
precision highp float;

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
@"#version 440
precision highp float;

in vec2 UV;
out vec4 fragColor;

float RangeFilter(float range_min, float range_max, float value)
{
    return step(range_min, value) * (1.0 - step(range_max, value));
}


vec3 Linear(float x0, float x1, vec3 y0, vec3 y1, float x)
{
    return (y1-y0)/(x1-x0)*(x-x0)+y0;
}

void main()
{
    const int num = 5;
    vec3[num] colors = vec3[](
        vec3(0.3686, 0.349, 0.3294),
        vec3(0.3686, 0.349, 0.3294),
        vec3(0.6314, 0.9882, 1.0),
        vec3(0.4902, 0.8902, 1.0),
        vec3(0.0902, 0.4863, 1.0)
    );
    float[num] offsets = float[]( 0.0, 0.498, 0.5, 0.54, 1.0 );
    vec3 color = vec3(0.0);
    for(int i = 0;i < num-1; i++) {
        color += RangeFilter(offsets[i], offsets[i+1], UV.y) * Linear(offsets[i], offsets[i+1], colors[i], colors[i+1], UV.y);
    }
    fragColor = vec4(color, 1.0);
}
";
    }
}
