#nullable enable
using Elffy.Core;
using Elffy.Diagnostics;
using Elffy.Components;
using Elffy.OpenGL;
using System;

namespace Elffy.Shading
{
    /// <summary>Simple shader which displays texture. (Target vertex type is <see cref="VertexSlim"/>)</summary>
    [ShaderTargetVertexType(typeof(VertexSlim))]
    public sealed class TextureShaderSource : ShaderSource
    {
        private static TextureShaderSource? _instance;

        /// <summary>Get singleton instance of <see cref="TextureShaderSource"/></summary>
        public static TextureShaderSource Instance => _instance ??= new();

        protected override string VertexShaderSource => VertSource;

        protected override string FragmentShaderSource => FragSource;

        protected override void DefineLocation(VertexDefinition definition, Renderable target)
        {
            definition.Map<VertexSlim>(nameof(VertexSlim.Position), "_pos");
            definition.Map<VertexSlim>(nameof(VertexSlim.UV), "_uv");
        }

        protected override void SendUniforms(Uniform uniform, Renderable target, ReadOnlySpan<Light> lights, in Matrix4 model, in Matrix4 view, in Matrix4 projection)
        {
            var to = target.TryGetComponent<Texture>(out var texture) ? texture.TextureObject :
                throw new InvalidOperationException($"{nameof(TextureShaderSource)} needs {nameof(Texture)} component.");
            uniform.SendTexture2D("_sampler", to, TextureUnitNumber.Unit0);
            uniform.Send("_mvp", projection * view * model);
        }

        private const string VertSource =
@"#version 440
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
@"#version 440
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
