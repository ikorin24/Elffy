#nullable enable
using Elffy.Core;
using Elffy.Diagnostics;
using System;
using System.Runtime.CompilerServices;

namespace Elffy.Shading
{
    [ShaderTargetVertexType(typeof(Vertex))]
    [ShaderTargetVertexType(typeof(VertexSlim))]
    public sealed class EmptyShaderSource<TVertex> : ShaderSource where TVertex : unmanaged
    {
        private static EmptyShaderSource<TVertex>? _instance;
        
        /// <summary>Get instance of <see cref="EmptyShaderSource{TVertex}"/></summary>
        public static EmptyShaderSource<TVertex> Instance
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if(_instance is not null) {
                    return _instance;
                }
                if(typeof(TVertex) == typeof(Vertex) ||
                   typeof(TVertex) == typeof(VertexSlim)) {

                    _instance = new EmptyShaderSource<TVertex>();
                    return _instance;
                }

                throw new NotSupportedException($"'{typeof(EmptyShaderSource<TVertex>)}' is not supported. The type must be '{nameof(Vertex)}' or '{nameof(VertexSlim)}'.");
            }
        }

        protected override string VertexShaderSource => VertSource;

        protected override string FragmentShaderSource => FragSource;

        private EmptyShaderSource()
        {
        }

        protected override void DefineLocation(VertexDefinition definition, Renderable target)
        {
            if(typeof(TVertex) == typeof(Vertex)) {
                definition.Map<Vertex>(nameof(Vertex.Position), "_pos");
            }
            else if(typeof(TVertex) == typeof(VertexSlim)) {
                definition.Map<VertexSlim>(nameof(VertexSlim.Position), "_pos");
            }
            else {
                throw new NotSupportedException($"{typeof(TVertex).FullName} is not supported.");
            }
        }

        protected override void SendUniforms(Uniform uniform, Renderable target, in Matrix4 model, in Matrix4 view, in Matrix4 projection)
        {
            uniform.Send("_mvp", projection * view * model);
        }

        private const string VertSource =
@"#version 440
in vec3 _pos;
uniform mat4 _mvp;
void main()
{
    gl_Position = _mvp * vec4(_pos, 1.0);
}
";
        private const string FragSource =
@"#version 440
out vec4 _fragColor;
void main()
{
    _fragColor = vec4(1.0, 1.0, 1.0, 1.0);
}
";
    }
}
