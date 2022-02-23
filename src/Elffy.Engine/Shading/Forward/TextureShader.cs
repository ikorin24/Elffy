#nullable enable
using Elffy.Graphics.OpenGL;
using System;

namespace Elffy.Shading.Forward
{
    /// <summary>Simple shader which displays texture.</summary>
    public sealed class TextureShader : ShaderSource
    {
        private ShaderTextureSelector<TextureShader>? _textureSelector;

        protected override string VertexShaderSource => VertSource;

        protected override string FragmentShaderSource => FragSource;

        public ShaderTextureSelector<TextureShader>? TextureSelector
        {
            get => _textureSelector;
            set => _textureSelector = value;
        }

        public TextureShader(ShaderTextureSelector<TextureShader>? textureSelector = null)
        {
            _textureSelector = textureSelector;
        }

        protected override void DefineLocation(VertexDefinition definition, Renderable target, Type vertexType)
        {
            definition.Map(vertexType, "_pos", VertexSpecialField.Position);
            definition.Map(vertexType, "_uv", VertexSpecialField.UV);
        }

        protected override void SendUniforms(ShaderDataDispatcher dispatcher, Renderable target, in Matrix4 model, in Matrix4 view, in Matrix4 projection)
        {
            var selector = _textureSelector ?? DefaultShaderTextureSelector<TextureShader>.Default;
            var hasTexture = selector.Invoke(this, target, out var texObj);

            dispatcher.SendUniform("_hasTexture", hasTexture);
            dispatcher.SendUniformTexture2D("_sampler", texObj, TextureUnitNumber.Unit0);
            dispatcher.SendUniform("_mvp", projection * view * model);
        }

        private const string VertSource =
@"#version 410
in vec3 _pos;
in vec2 _uv;
uniform mat4 _mvp;
out vec2 _vUV;
void main()
{
    gl_Position = _mvp * vec4(_pos, 1.0);
    _vUV = _uv;
}
";

        private const string FragSource =
@"#version 410
in vec2 _vUV;
uniform sampler2D _sampler;
uniform bool _hasTexture;
out vec4 _fragColor;
void main()
{
    if(_hasTexture) {
        _fragColor = texture(_sampler, _vUV);
    }
    else {
        _fragColor = vec4(1.0, 0.0, 1.0, 1.0);
    }
}
";
    }
}
