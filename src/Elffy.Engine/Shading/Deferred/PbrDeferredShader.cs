﻿#nullable enable
using System;
using Elffy.Components;
using Elffy.Graphics.OpenGL;

namespace Elffy.Shading.Deferred
{
    public sealed class PbrDeferredShader : ShaderSource
    {
        private ShaderTextureSelector<PbrDeferredShader>? _textureSelector;
        private Color3 _albedo;
        private float _metallic;
        private float _roughness;
        private float _emit;

        public ref Color3 Albedo => ref _albedo;
        public float Metallic { get => _metallic; set => _metallic = value; }
        public float Roughness { get => _roughness; set => _roughness = value; }
        public float Emit { get => _emit; set => _emit = value; }

        public ShaderTextureSelector<PbrDeferredShader>? TextureSelector { get => _textureSelector; set => _textureSelector = value; }

        protected override string VertexShaderSource => VertSource;

        protected override string FragmentShaderSource => FragSource;

        public PbrDeferredShader(ShaderTextureSelector<PbrDeferredShader>? textureSelector = null)
        {
            _textureSelector = textureSelector;
        }

        public PbrDeferredShader(Color3 albedo, float metallic, float roughness, float emit, ShaderTextureSelector<PbrDeferredShader>? textureSelector = null)
        {
            _albedo = albedo;
            _metallic = metallic;
            _roughness = roughness;
            _emit = emit;
            _textureSelector = textureSelector;
        }

        protected override void DefineLocation(VertexDefinition definition, Renderable target, Type vertexType)
        {
            definition.Map(vertexType, "_vPos", VertexSpecialField.Position);
            definition.Map(vertexType, "_vNormal", VertexSpecialField.Normal);
            definition.Map(vertexType, "_vUV", VertexSpecialField.UV);
        }

        protected override void SendUniforms(Uniform uniform, Renderable target, in Matrix4 model, in Matrix4 view, in Matrix4 projection)
        {
            uniform.Send("_model", model);
            uniform.Send("_view", view);
            uniform.Send("_mvp", projection * view * model);
            uniform.Send("_albedoMetallic", new Color4(_albedo, _metallic));
            uniform.Send("_emitRoughness", new Color4(_emit, _emit, _emit, _roughness));

            var selector = _textureSelector ?? DefaultShaderTextureSelector<PbrDeferredShader>.Default;
            var hasTexture = selector.Invoke(this, target, out var texObj);
            uniform.SendTexture2D("_tex", texObj, TextureUnitNumber.Unit0);
            uniform.Send("_hasTexture", hasTexture);
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

        private const string FragSource =
@"#version 410
in vec3 _pos;
in vec3 _normal;
in vec2 _uv;
uniform mat4 _model;
uniform mat4 _view;
uniform vec4 _albedoMetallic;
uniform vec4 _emitRoughness;
uniform sampler2D _tex;
uniform bool _hasTexture;
layout (location = 0) out vec4 _gPosition;          // (x, y, z, 1)
layout (location = 1) out vec4 _gNormal;            // (normal.x, normal.y, normal.z, 1)
layout (location = 2) out vec4 _gAlbedo;            // (albedo.r, albedo.g, albedo.b, 1)
layout (location = 3) out vec4 _gEmit;              // (emit.r, emit.g, emit.b, 1)
layout (location = 4) out vec4 _gMetallicRoughness; // (metallic, roughness, 0, 1)

void main()
{
    _gPosition = _model * vec4(_pos, 1.0);
    _gNormal = vec4(normalize(transpose(inverse(mat3(_model))) * _normal), 1.0);
    _gAlbedo = _hasTexture ? vec4(texture(_tex, _uv).rgb, 1.0) : vec4(_albedoMetallic.rgb, 1.0);
    _gEmit = vec4(_emitRoughness.rgb, 1.0);
    _gMetallicRoughness = vec4(_albedoMetallic.a, _emitRoughness.a, 0.0, 1.0);
}
";
    }
}