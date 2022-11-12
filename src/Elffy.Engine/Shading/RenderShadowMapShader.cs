#nullable enable
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

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

    public void DispatchShader(ShaderDataDispatcher dispatcher, in ShadowMapRenderingContext context)
    {
        var shadowMap = context.ShadowMap;
        dispatcher.SendUniform("_model", context.Model);
        dispatcher.SendUniformTexture1D("_lightMatData", shadowMap.LightMatricesDataTexture, 0);
    }

    void IRenderingShader.OnProgramDisposedInternal() { }     // nop

    void IRenderingShader.OnAttachedInternal(Renderable target) { }   // nop

    void IRenderingShader.OnDetachedInternal(Renderable target) { }   // nop

    ShaderSource IRenderingShader.GetShaderSourceInternal(in ShaderGetterContext context)
    {
        var light = context.Screen.RenderPipeline.Lights[0];    // TODO:
        var cascadeCount = light.ShadowMap.CascadeCount;
        if(cascadeCount == 1) {
            return GetNonCascadedShaderSource();
        }
        else {
            return GetCascadedShaderSource(cascadeCount);
        }
    }

    private static ShaderSource GetNonCascadedShaderSource()
    {
        return new()
        {
            OnlyContainsConstLiteralUtf8 = true,
            VertexShader =
            """
            #version 410
            layout (location = 0) in vec3 _vPos;
            uniform mat4 _model;
            uniform sampler1D _lightMatData;
            void main()
            {
                mat4 lightMat = mat4(
                    texelFetch(_lightMatData, 0, 0),
                    texelFetch(_lightMatData, 1, 0),
                    texelFetch(_lightMatData, 2, 0),
                    texelFetch(_lightMatData, 3, 0));
                gl_Position = lightMat * _model * vec4(_vPos, 1.0);
            }
            """u8,
            FragmentShader =
            """
            #version 410
            void main(){}
            """u8,
        };
    }

    private static ShaderSource GetCascadedShaderSource(int cascadedCount)
    {
        var geometryShader = cascadedCount switch
        {
            2 => """
            #version 460
            #define LightCascadedCount 2
            layout(triangles, invocations = LightCascadedCount) in;
            layout(triangle_strip, max_vertices = 3) out;
            uniform sampler1D _lightMatData;
            void main()
            {
                mat4 lightMat = mat4(
                    texelFetch(_lightMatData, gl_InvocationID * 4,     0),
                    texelFetch(_lightMatData, gl_InvocationID * 4 + 1, 0),
                    texelFetch(_lightMatData, gl_InvocationID * 4 + 2, 0),
                    texelFetch(_lightMatData, gl_InvocationID * 4 + 3, 0));
                for(int i = 0; i < 3; ++i) {
                    gl_Position = lightMat * gl_in[i].gl_Position;
                    gl_Layer = gl_InvocationID;
                    EmitVertex();
                }
            }
            """u8,
            3 => """
            #version 460
            #define LightCascadedCount 3
            layout(triangles, invocations = LightCascadedCount) in;
            layout(triangle_strip, max_vertices = 3) out;
            uniform sampler1D _lightMatData;
            void main()
            {
                mat4 lightMat = mat4(
                    texelFetch(_lightMatData, gl_InvocationID * 4,     0),
                    texelFetch(_lightMatData, gl_InvocationID * 4 + 1, 0),
                    texelFetch(_lightMatData, gl_InvocationID * 4 + 2, 0),
                    texelFetch(_lightMatData, gl_InvocationID * 4 + 3, 0));
                for(int i = 0; i < 3; ++i) {
                    gl_Position = lightMat * gl_in[i].gl_Position;
                    gl_Layer = gl_InvocationID;
                    EmitVertex();
                }
            }
            """u8,
            4 => """
            #version 460
            #define LightCascadedCount 4
            layout(triangles, invocations = LightCascadedCount) in;
            layout(triangle_strip, max_vertices = 3) out;
            uniform sampler1D _lightMatData;
            void main()
            {
                mat4 lightMat = mat4(
                    texelFetch(_lightMatData, gl_InvocationID * 4,     0),
                    texelFetch(_lightMatData, gl_InvocationID * 4 + 1, 0),
                    texelFetch(_lightMatData, gl_InvocationID * 4 + 2, 0),
                    texelFetch(_lightMatData, gl_InvocationID * 4 + 3, 0));
                for(int i = 0; i < 3; ++i) {
                    gl_Position = lightMat * gl_in[i].gl_Position;
                    gl_Layer = gl_InvocationID;
                    EmitVertex();
                }
            }
            """u8,
            5 => """
            #version 460
            #define LightCascadedCount 5
            layout(triangles, invocations = LightCascadedCount) in;
            layout(triangle_strip, max_vertices = 3) out;
            uniform sampler1D _lightMatData;
            void main()
            {
                mat4 lightMat = mat4(
                    texelFetch(_lightMatData, gl_InvocationID * 4,     0),
                    texelFetch(_lightMatData, gl_InvocationID * 4 + 1, 0),
                    texelFetch(_lightMatData, gl_InvocationID * 4 + 2, 0),
                    texelFetch(_lightMatData, gl_InvocationID * 4 + 3, 0));
                for(int i = 0; i < 3; ++i) {
                    gl_Position = lightMat * gl_in[i].gl_Position;
                    gl_Layer = gl_InvocationID;
                    EmitVertex();
                }
            }
            """u8,
            <= 1 or > DirectLightConfig.MaxCascadeCount => throw new NotSupportedException("too many cascades or invalid"),
        };

        return new()
        {
            OnlyContainsConstLiteralUtf8 = true,
            VertexShader =
            """
            #version 410
            layout (location = 0) in vec3 _vPos;
            uniform mat4 _model;
            void main()
            {
                gl_Position = _model * vec4(_vPos, 1.0);
            }
            """u8,
            GeometryShader = geometryShader,
            FragmentShader =
            """
            #version 410
            void main(){}
            """u8,
        };
    }
}

internal readonly ref struct ShadowMapRenderingContext
{
    private readonly IHostScreen _screen;
    private readonly ObjectLayer _layer;
    private readonly Renderable _target;
    private readonly CascadedShadowMap _shadowMap;
    private readonly ref readonly Matrix4 _model;

    public IHostScreen Screen => _screen;
    public RenderPipeline RenderPipeline => _screen.RenderPipeline;
    public ObjectLayer Layer => _layer;
    public Renderable Target => _target;
    public CascadedShadowMap ShadowMap => _shadowMap;
    public ref readonly Matrix4 Model => ref _model;

    [Obsolete("Don't use defaut constructor.", true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public ShadowMapRenderingContext() => throw new NotSupportedException("Don't use defaut constructor.");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ShadowMapRenderingContext(
        IHostScreen screen,
        ObjectLayer layer,
        Renderable target,
        CascadedShadowMap shadowMap,
        in Matrix4 model)
    {
        _screen = screen;
        _layer = layer;
        _target = target;
        _shadowMap = shadowMap;
        _model = ref model;
    }
}
