#nullable enable
using Elffy.Core;
using Elffy.Diagnostics;
using Elffy.Components;
using Elffy.OpenGL;
using System;
using System.Runtime.CompilerServices;

namespace Elffy.Shading
{
    /// <summary>Simple shader which displays texture.</summary>
    [ShaderTargetVertexType(typeof(VertexSlim))]
    [ShaderTargetVertexType(typeof(Vertex))]
    public sealed class TextureShaderSource<TVertex> : ShaderSource
    {
        private static TextureShaderSource<TVertex>? _instance;

        /// <summary>Get singleton instance of <see cref="TextureShaderSource"/></summary>
        public static TextureShaderSource<TVertex> Instance
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if(_instance is not null) {
                    return _instance;
                }
                if(typeof(TVertex) == typeof(VertexSlim) || typeof(TVertex) == typeof(Vertex)) {
                    return _instance = new();
                }
                throw new NotSupportedException("Not supported vertex type.");
            }
        }

        protected override string VertexShaderSource => VertSource;

        protected override string FragmentShaderSource => FragSource;

        protected override void DefineLocation(VertexDefinition definition, Renderable target)
        {
            if(typeof(TVertex) == typeof(VertexSlim)) {
                definition.Map<VertexSlim>(nameof(VertexSlim.Position), "_pos");
                definition.Map<VertexSlim>(nameof(VertexSlim.UV), "_uv");
            }
            else if(typeof(TVertex) == typeof(Vertex)) {
                definition.Map<Vertex>(nameof(Vertex.Position), "_pos");
                definition.Map<Vertex>(nameof(Vertex.UV), "_uv");
            }
            else {
                throw new NotSupportedException("Not supported vertex type.");
            }
        }

        protected override void SendUniforms(Uniform uniform, Renderable target, in Matrix4 model, in Matrix4 view, in Matrix4 projection)
        {
            var texture = target.GetComponent<Texture>();

            uniform.SendTexture2D("_sampler", texture.TextureObject, TextureUnitNumber.Unit0);
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
