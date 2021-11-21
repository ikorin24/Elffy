#nullable enable
using System;

namespace Elffy.Shading.Forward
{
    internal sealed class EmptyShader : ShaderSource
    {
        private static EmptyShader? _instance;

        /// <summary>Get singleton instance</summary>
        public static EmptyShader Instance => _instance ??= new();

        public override string VertexShaderSource => VertSource;

        public override string FragmentShaderSource => FragSource;

        private EmptyShader()
        {
        }

        protected override void DefineLocation(VertexDefinition definition, Renderable target, Type vertexType)
        {
            definition.Map(vertexType, "_pos", VertexSpecialField.Position);
        }

        protected override void SendUniforms(Uniform uniform, Renderable target, in Matrix4 model, in Matrix4 view, in Matrix4 projection)
        {
            uniform.Send("_mvp", projection * view * model);
        }

        private const string VertSource =
@"#version 410
in vec3 _pos;
uniform mat4 _mvp;
void main()
{
    gl_Position = _mvp * vec4(_pos, 1.0);
}
";
        private const string FragSource =
@"#version 410
out vec4 _fragColor;
void main()
{
    _fragColor = vec4(1.0, 0.0, 1.0, 1.0);
}
";
    }
}
