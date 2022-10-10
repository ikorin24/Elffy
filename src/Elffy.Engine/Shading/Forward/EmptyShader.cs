#nullable enable
using System;

namespace Elffy.Shading.Forward;

internal sealed class EmptyShader : RenderingShader
{
    private static EmptyShader? _instance;

    /// <summary>Get singleton instance</summary>
    public static EmptyShader Instance => _instance ??= new();

    private EmptyShader()
    {
    }

    protected override ShaderSource GetShaderSource(in ShaderGetterContext context)
    {
        return new()
        {
            OnlyContainsConstLiteralUtf8 = true,
            VertexShader =
"""
#version 410
in vec3 _pos;
uniform mat4 _mvp;
void main()
{
    gl_Position = _mvp * vec4(_pos, 1.0);
}
"""u8,
            FragmentShader =
"""
#version 410
out vec4 _fragColor;
void main()
{
    _fragColor = vec4(1.0, 0.0, 1.0, 1.0);
}
"""u8,
        };
    }

    protected override void DefineLocation(VertexDefinition definition, in LocationDefinitionContext context)
    {
        definition.Map(context.VertexType, "_pos", VertexFieldSemantics.Position);
    }

    protected override void OnRendering(ShaderDataDispatcher dispatcher, in RenderingContext context)
    {
        dispatcher.SendUniform("_mvp", context.Projection * context.View * context.Model);
    }
}
