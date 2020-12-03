#nullable enable
using Elffy.Core;
using Elffy.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elffy.Shading
{
    [ShaderTargetVertexType(typeof(Vertex))]
    [ShaderTargetVertexType(typeof(VertexSlim))]
    public sealed class EmptyShaderSource<TVertex> : ShaderSource where TVertex : unmanaged
    {
        private static EmptyShaderSource<TVertex>? _instance;
        public static EmptyShaderSource<TVertex> Instance => _instance ??= new EmptyShaderSource<TVertex>();

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
                throw new NotSupportedException($"{typeof(Vertex).FullName} and {typeof(VertexSlim).FullName} is only supported type.");
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
