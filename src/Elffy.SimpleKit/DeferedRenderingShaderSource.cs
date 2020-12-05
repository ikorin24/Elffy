#nullable enable
using System;
using Elffy.Components;
using Elffy.Diagnostics;
using Elffy.Core;

namespace Elffy.Shading
{
    [ShaderTargetVertexType(typeof(Vertex))]
    public sealed class DeferedRenderingShaderSource : ShaderSource
    {
        private static DeferedRenderingShaderSource? _instance;
        public static DeferedRenderingShaderSource Instance => _instance ??= new DeferedRenderingShaderSource();

        protected override string VertexShaderSource => VertSource;

        protected override string FragmentShaderSource => FragSource;

        private DeferedRenderingShaderSource()
        {
        }

        protected override void DefineLocation(VertexDefinition definition, Renderable target)
        {
            definition.Map<Vertex>(nameof(Vertex.Position), "_vPos");
            definition.Map<Vertex>(nameof(Vertex.Normal), "_vNormal");
        }

        protected override void SendUniforms(Uniform uniform, Renderable target, in Matrix4 model, in Matrix4 view, in Matrix4 projection)
        {
            var material = target.TryGetComponent<Material>(out var m) ? m.Value : MaterialValue.GreenRubber;
            uniform.Send("_diffuse", material.Diffuse);
            uniform.Send("_specular", material.Specular.G);

            var modelView = view * model;
            uniform.Send("_modelView", modelView);
            uniform.Send("_mvp", projection * modelView);
        }

        private const string VertSource =
@"#version 440
in vec3 _vPos;
in vec3 _vNormal;
uniform mat4 _mvp;
out vec3 _pos;
out vec3 _normal;
out vec2 _uv;
void main()
{
    _pos = _vPos;
    _normal =  _vNormal;
    gl_Position = _mvp * vec4(_pos, 1.0);
}";

        private const string FragSource =
@"#version 440
in vec3 _pos;
in vec3 _normal;
uniform mat4 _modelView;
uniform vec4 _diffuse;
uniform float _specular;
layout (location = 0) out vec4 _gPosition;
layout (location = 1) out vec4 _gNormal;
layout (location = 2) out vec4 _gColor;

void main()
{    
    _gPosition = vec4((_modelView * vec4(_pos, 1.0)).xyz, 1.0);            // position in eye space
    _gNormal = vec4(transpose(inverse(mat3(_modelView))) * _normal, 1.0);  // normal in world coordinate
    _gColor.rgb = _diffuse.rgb;
    _gColor.a = _specular;
}  
";
    }
}
