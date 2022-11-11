#nullable enable
using System;

namespace Elffy.Shading;

internal sealed class RenderShadowMapShader : IRenderingShader
{
    public static RenderShadowMapShader Instance { get; } = new RenderShadowMapShader();

    private RenderShadowMapShader()
    {
    }

    public void DefineLocation(VertexDefinition definition, Renderable target, Type vertexType)
    {
        definition.Map(vertexType, 0, VertexFieldSemantics.Position);
    }

    public void DispatchShader(ShaderDataDispatcher dispatcher, in Matrix4 model, in Matrix4 lightViewProjection)
    {
        dispatcher.SendUniform("_lmvp", lightViewProjection * model);
    }

    void IRenderingShader.OnProgramDisposedInternal() { }     // nop

    void IRenderingShader.OnAttachedInternal(Renderable target) { }   // nop

    void IRenderingShader.OnDetachedInternal(Renderable target) { }   // nop

    ShaderSource IRenderingShader.GetShaderSourceInternal(in ShaderGetterContext context) => new()
    {
        OnlyContainsConstLiteralUtf8 = true,
        VertexShader =
        """
        #version 410
        layout (location = 0) in vec3 _vPos;
        uniform mat4 _lmvp;
        void main()
        {
            gl_Position = _lmvp * vec4(_vPos, 1.0);
        }
        """u8,
        FragmentShader =
        """
        #version 410
        void main(){}
        """u8,
    };

    //private static ReadOnlySpan<byte> Geom => """
    //    #version 460
    //    layout(triangles, invocations = 5) in;
    //    layout(triangle_strip, max_vertices = 3) out;
    //    layout (std140, binding = 0) uniform LightSpaceMatrices
    //    {
    //        mat4 lightSpaceMatrices[16];
    //    };
    //    void main()
    //    {
    //        for(int i = 0; i < 3; ++i) {
    //            gl_Position = lightSpaceMatrices[gl_InvocationID] * gl_in[i].gl_Position;
    //        }
    //    }
    //    """u8;
}
