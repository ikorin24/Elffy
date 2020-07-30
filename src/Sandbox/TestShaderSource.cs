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
            target.GetComponent<ShaderStorage>().BindIndex(index: 0);
        }

        private const string VertexShader =
@"#version 440
in vec3 _pos;
in ivec4 _bone;
uniform mat4 _mvp;

//struct V3
//{
//    float x;
//    float y;
//    float z;
//};

//layout(std430,binding=0) readonly buffer SSBO
//{
//    V3 _ssbo[];
//};

layout(std430,binding=0) readonly buffer SSBO
{
    vec4 _ssbo[];
};

void main()
{
    //gl_Position = _mvp * vec4(_pos, 1.0);
    int i = 0;
    //gl_Position = _mvp * vec4(_pos + _ssbo[0].xyz, 1.0);
    gl_Position = _mvp * vec4(_pos + float(_bone.y), 1.0);
}
";

        private const string FragmentShader =
@"#version 440
out vec4 fragColor;

void main()
{
    fragColor = vec4(1.0);
}
";
    }
}
