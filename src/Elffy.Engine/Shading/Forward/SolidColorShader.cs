﻿#nullable enable
using System;

namespace Elffy.Shading.Forward;

public sealed class SolidColorShader : RenderingShader
{
    private Color4 _color;

    public Color4 Color { get => _color; set => _color = value; }

    public SolidColorShader()
    {
    }

    protected override void DefineLocation(VertexDefinition definition, in LocationDefinitionContext context)
    {
        definition.Map(context.VertexType, "_pos", VertexFieldSemantics.Position);
    }

    protected override void OnRendering(ShaderDataDispatcher dispatcher, in RenderingContext context)
    {
        dispatcher.SendUniform("_mvp", context.Projection * context.View * context.Model);
        dispatcher.SendUniform("_color", _color);
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
uniform vec4 _color;
void main()
{
    _fragColor = _color;
}
"""u8,
        };
    }
}
