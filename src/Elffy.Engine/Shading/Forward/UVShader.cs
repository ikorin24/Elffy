#nullable enable
using System;

namespace Elffy.Shading.Forward;

public sealed class UVShader : RenderingShader
{
    protected override void DefineLocation(VertexDefinition definition, in LocationDefinitionContext context)
    {
        definition.Map(context.VertexType, "_pos", VertexSpecialField.Position);
        definition.Map(context.VertexType, "_uv", VertexSpecialField.UV);
    }

    protected override void OnRendering(ShaderDataDispatcher dispatcher, in RenderingContext context)
    {
        dispatcher.SendUniform("_mvp", context.Projection * context.View * context.Model);
    }

    protected override ShaderSource GetShaderSource(in ShaderGetterContext context) => new()
    {
        OnlyContainsConstLiteralUtf8 = true,
        VertexShader =
        """
        #version 410
        in vec3 _pos;
        in vec2 _uv;
        out vec2 _vUV;
        uniform mat4 _mvp;
        void main()
        {
            _vUV = _uv;
            gl_Position = _mvp * vec4(_pos, 1.0);
        }
        """u8,
        FragmentShader =
        """
        #version 410
        in vec2 _vUV;
        out vec4 _fragColor;
        void main()
        {
            _fragColor = vec4(_vUV, 0.0, 1.0);
        }
        """u8,
    };
}
