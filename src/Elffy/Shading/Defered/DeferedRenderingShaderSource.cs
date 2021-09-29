#nullable enable
using System;
using Elffy.Components;
using Elffy.Core;
using Elffy.Graphics.OpenGL;

namespace Elffy.Shading.Defered
{
    public sealed class DeferedRenderingShaderSource : ShaderSource
    {
        private static DeferedRenderingShaderSource? _instance;
        public static DeferedRenderingShaderSource Instance => _instance ??= new DeferedRenderingShaderSource();

        public override string VertexShaderSource => VertSource;

        public override string FragmentShaderSource => FragSource;

        private DeferedRenderingShaderSource()
        {
        }

        protected override void DefineLocation(VertexDefinition definition, Renderable target, Type vertexType)
        {
            definition.Map(vertexType, "_vPos", VertexSpecialField.Position);
            definition.Map(vertexType, "_vNormal", VertexSpecialField.Normal);
            definition.Map(vertexType, "_vUV", VertexSpecialField.UV);
        }

        protected override void SendUniforms(Uniform uniform, Renderable target, in Matrix4 model, in Matrix4 view, in Matrix4 projection)
        {
            var modelView = view * model;
            uniform.Send("_modelView", modelView);
            uniform.Send("_mvp", projection * modelView);

            var hasTexture = target.TryGetComponent<Texture>(out var texture);
            var texObj = hasTexture ? texture!.TextureObject : TextureObject.Empty;
            uniform.SendTexture2D("_tex", texObj, TextureUnitNumber.Unit0);
            uniform.Send("_hasTexture", hasTexture);
        }

        private const string VertSource =
@"#version 410
in vec3 _vPos;
in vec3 _vNormal;
in vec2 _vUV;
uniform mat4 _mvp;
out vec3 _pos;
out vec3 _normal;
out vec2 _uv;
void main()
{
    _pos = _vPos;
    _normal = _vNormal;
    _uv = _vUV;
    gl_Position = _mvp * vec4(_pos, 1.0);
}";

        private const string FragSource =
@"#version 410
in vec3 _pos;
in vec3 _normal;
in vec2 _uv;
uniform mat4 _modelView;
uniform sampler2D _tex;
uniform bool _hasTexture;
layout (location = 0) out vec4 _gPosition;
layout (location = 1) out vec4 _gNormal;
layout (location = 2) out vec4 _gColor;

void main()
{
    _gPosition = _modelView * vec4(_pos, 1.0);                             // position in eye space
    _gNormal = vec4(transpose(inverse(mat3(_modelView))) * _normal, 1.0);  // normal in eye space
    _gColor = _hasTexture ? texture(_tex, _uv) : vec4(1.0, 1.0, 1.0, 1.0);
}  
";
    }
}
