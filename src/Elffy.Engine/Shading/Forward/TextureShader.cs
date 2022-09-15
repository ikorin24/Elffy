#nullable enable
using Elffy;
using Elffy.Graphics.OpenGL;
using System;

namespace Elffy.Shading.Forward
{
    /// <summary>Simple shader which displays texture.</summary>
    public sealed class TextureShader : RenderingShader
    {
        private Texture? _texture;

        public Texture? Texture { get => _texture; set => _texture = value; }

        public TextureShader()
        {
        }

        protected override void OnProgramDisposed()
        {
            _texture?.Dispose();
            _texture = null;
            base.OnProgramDisposed();
        }

        protected override void DefineLocation(VertexDefinition definition, Renderable target, Type vertexType)
        {
            definition.Map(vertexType, "_pos", VertexSpecialField.Position);
            definition.Map(vertexType, "_uv", VertexSpecialField.UV);
        }

        protected override void OnRendering(ShaderDataDispatcher dispatcher, Renderable target, in Matrix4 model, in Matrix4 view, in Matrix4 projection)
        {
            var texture = _texture;
            dispatcher.SendUniform("_hasTexture", texture != null);
            if(texture != null) {
                dispatcher.SendUniformTexture2D("_sampler", texture.TextureObject, TextureUnitNumber.Unit0);
            }
            dispatcher.SendUniform("_mvp", projection * view * model);
        }

        protected override ShaderSource GetShaderSource(Renderable target, ObjectLayer layer) => new()
        {
            VertexShader =
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
",
            FragmentShader =
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
",
        };
    }
}
