#nullable enable
using Elffy.Core;
using Elffy.Components;
using Elffy.OpenGL;
using System;

namespace Elffy.Shading.Forward
{
    /// <summary>Simple shader which displays texture.</summary>
    public sealed class ColorTextureShader : ShaderSource
    {
        private static ColorTextureShader? _instance;

        /// <summary>Get singleton instance</summary>
        public static ColorTextureShader Instance => _instance ??= new();

        public override string VertexShaderSource => VertSource;

        public override string FragmentShaderSource => FragSource;

        private ColorTextureShader() { }

        protected override void DefineLocation(VertexDefinition definition, Renderable target, Type vertexType)
        {
            definition.Map(vertexType, "_pos", VertexSpecialField.Position);
            definition.Map(vertexType, "_uv", VertexSpecialField.UV);
        }

        protected override void SendUniforms(Uniform uniform, Renderable target, in Matrix4 model, in Matrix4 view, in Matrix4 projection)
        {
            var texture = target.GetComponent<Texture>();

            uniform.SendTexture2D("_sampler", texture.TextureObject, TextureUnitNumber.Unit0);
            uniform.Send("_mvp", projection * view * model);
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
out vec4 _fragColor;
void main()
{
    _fragColor = texture(_sampler, _vUV);
}
";
    }
}
