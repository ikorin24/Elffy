#nullable enable
using System;
using Elffy.Graphics.OpenGL;

namespace Elffy.Shading.Deferred
{
    public sealed class PbrDeferredShader : RenderingShader
    {
        private Texture? _texture;
        private Color3 _baseColor;
        private float _metallic;
        private float _roughness;

        public ref Color3 BaseColor => ref _baseColor;
        public float Metallic { get => _metallic; set => _metallic = value; }
        public float Roughness { get => _roughness; set => _roughness = value; }
        public Texture? Texture { get => _texture; set => _texture = value; }

        public PbrDeferredShader()
        {
        }

        public PbrDeferredShader(Color3 albedo, float metallic, float roughness)
        {
            _baseColor = albedo;
            _metallic = metallic;
            _roughness = roughness;
        }

        protected override void OnProgramDisposed()
        {
            _texture?.Dispose();
            _texture = null;
            base.OnProgramDisposed();
        }

        protected override void DefineLocation(VertexDefinition definition, Renderable target, Type vertexType)
        {
            definition.Map(vertexType, "_vPos", VertexSpecialField.Position);
            definition.Map(vertexType, "_vNormal", VertexSpecialField.Normal);
            definition.Map(vertexType, "_vUV", VertexSpecialField.UV);
        }

        protected override void OnRendering(ShaderDataDispatcher dispatcher, Renderable target, in Matrix4 model, in Matrix4 view, in Matrix4 projection)
        {
            dispatcher.SendUniform("_model", model);
            dispatcher.SendUniform("_view", view);
            dispatcher.SendUniform("_mvp", projection * view * model);
            dispatcher.SendUniform("_baseColorMetallic", new Color4(_baseColor, _metallic));
            dispatcher.SendUniform("_roughness", _roughness);
            var texture = _texture;
            if(texture != null) {
                dispatcher.SendUniformTexture2D("_tex", texture.TextureObject, TextureUnitNumber.Unit0);
            }
            dispatcher.SendUniform("_hasTexture", texture != null);
        }

        protected override ShaderSource GetShaderSource(Renderable target, ObjectLayer layer)
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
in vec3 _vNormal;
in vec2 _vUV;
uniform mat4 _mvp;
uniform float _metallic;
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

        // index  | R           | G            | B           | A         |
        // ----
        // mrt[0] | pos.x       | pos.y        | pos.z       | 1         |
        // mrt[1] | normal.x    | normal.y     | normal.z    | roughness |
        // mrt[2] | baseColor.r | baseColor.g  | baseColor.b | metallic  |
        // mrt[3] | 0           | 0            | 0           | 0         |
        // mrt[4] | 0           | 0            | 0           | 0         |

        private const string FragSource =
@"#version 410
in vec3 _pos;
in vec3 _normal;
in vec2 _uv;
uniform mat4 _model;
uniform mat4 _view;
uniform vec4 _baseColorMetallic;
uniform float _roughness;
uniform sampler2D _tex;
uniform bool _hasTexture;
layout (location = 0) out vec4 _mrt0;
layout (location = 1) out vec4 _mrt1;
layout (location = 2) out vec4 _mrt2;
layout (location = 3) out vec4 _mrt3;
layout (location = 4) out vec4 _mrt4;

vec3 ToVec3(vec4 v)
{
    return v.xyz / v.w;
}

void main()
{
    _mrt0 = vec4(ToVec3(_model * vec4(_pos, 1.0)).xyz, 1.0);
    _mrt1 = vec4(normalize(transpose(inverse(mat3(_model))) * _normal), _roughness);
    if(_hasTexture) {
        _mrt2 = vec4(texture(_tex, _uv).rgb, _baseColorMetallic.a);
    }
    else {
        _mrt2 = _baseColorMetallic;
    }
    _mrt3 = vec4(0, 0, 0, 0);
    _mrt4 = vec4(0, 0, 0, 0);
}
";
    }
}
