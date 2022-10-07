#nullable enable
using System;

namespace Elffy.Shading.Forward;

public sealed class SolidColorShader : RenderingShader
{
    private Color4 _color;

    public Color4 Color { get => _color; set => _color = value; }

    public SolidColorShader()
    {
    }

    protected override void DefineLocation(VertexDefinition definition, Renderable target, Type vertexType)
    {
        definition.Map(vertexType, "_pos", VertexSpecialField.Position);
    }

    protected override void OnRendering(ShaderDataDispatcher dispatcher, Renderable target, in Matrix4 model, in Matrix4 view, in Matrix4 projection)
    {
        dispatcher.SendUniform("_mvp", projection * view * model);
        dispatcher.SendUniform("_color", _color);
    }

    protected override ShaderSource GetShaderSource(Renderable target, ObjectLayer layer)
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
uniform vec4 _color;
void main()
{
    _fragColor = _color;
}
"""u8,
        };
    }
}
