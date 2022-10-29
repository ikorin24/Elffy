#nullable enable
using Cysharp.Threading.Tasks;
using Elffy.Shading;
using System;
using System.Diagnostics;
using System.Threading;

namespace Elffy;

public sealed class DirectLight : ILight
{
    private const int DefaultShadowMapSize = 2048;
    private LightImpl _impl;
    private LifeState _state;
    private AsyncEventSource<DirectLight>? _terminating;
    private EventSource<DirectLight>? _dead;

    public AsyncEvent<DirectLight> Terminating => new(ref _terminating);
    public Event<DirectLight> Dead => new(ref _dead);

    public LifeState LifeState => _state;

    short ILight.Token => _impl.Token;

    int ILight.Index => _impl.Index;

    Vector4 ILight.Position
    {
        get => _impl.GetPosition().DereferOrDefault();
        set => _impl.TrySetPosition(value);
    }

    public Vector3 Direction
    {
        get => _impl.GetPosition().TryDerefer(out var pos) ?
            (pos.W != 0) ? (-pos.Xyz / pos.W) : default :
            default;
        set => _impl.TrySetPosition(new Vector4(-value, 0));
    }

    public Color4 Color
    {
        get => _impl.GetColor().DereferOrDefault();
        set => _impl.TrySetColor(value);
    }

    public RefReadOnlyOrNull<Matrix4> LightMatrix => _impl.GetLightMatrix();

    public RefReadOnlyOrNull<ShadowMapData> ShadowMap => _impl.GetShadowMap();

    internal DirectLight(LightManager lightManager, int index, short token)
    {
        _impl = new LightImpl(lightManager, index, token);
        _state = LifeState.New;
    }

    public UniTask Terminate() => Terminate(FrameTiming.Update);

    public async UniTask Terminate(FrameTiming timing)
    {
        var screen = _impl.Manager.Screen;
        screen.ThrowIfCurrentTimingOufOfFrame();

        if(_state >= LifeState.Terminating) {
            throw new InvalidOperationException($"Cannot terminate {nameof(DirectLight)} twice.");
        }

        Debug.Assert(_state == LifeState.Alive);
        var manager = _impl.Manager;
        if(timing == FrameTiming.NotSpecified) {
            timing = FrameTiming.Update;
        }
        var timings = manager.Screen.Timings;
        var endTiming = timings.GetTiming(timing);

        _state = LifeState.Terminating;

        try {
            await _terminating.InvokeIfNotNull(this, CancellationToken.None);
            await timings.FrameFinalizing.NextOrNow();
        }
        catch {
            if(EngineSetting.UserCodeExceptionCatchMode == UserCodeExceptionCatchMode.Throw) { throw; }
        }
        finally {
            manager.RemoveLight(this);
        }
        try {
            _dead?.Invoke(this);
        }
        catch {
            if(EngineSetting.UserCodeExceptionCatchMode == UserCodeExceptionCatchMode.Throw) { throw; }
        }
        _state = LifeState.Dead;
        await timings.GetTiming(timing).NextFrame();
    }

    public IHostScreen GetValidScreen() => _impl.Manager.Screen;

    public static UniTask<DirectLight> Create(IHostScreen screen, Vector3 direction, Color4 color)
    {
        ArgumentNullException.ThrowIfNull(screen);
        return Create(screen.Lights, direction, color, true, FrameTiming.Update);
    }

    public static UniTask<DirectLight> Create(IHostScreen screen, Vector3 direction, Color4 color, bool useShadowMap)
    {
        ArgumentNullException.ThrowIfNull(screen);
        return Create(screen.Lights, direction, color, useShadowMap, FrameTiming.Update);
    }

    public static UniTask<DirectLight> Create(IHostScreen screen, Vector3 direction, Color4 color, bool useShadowMap, FrameTiming timing)
    {
        ArgumentNullException.ThrowIfNull(screen);
        return Create(screen.Lights, direction, color, useShadowMap, timing);
    }

    public static UniTask<DirectLight> Create(LightManager manager, Vector3 direction, Color4 color)
    {
        return Create(manager, direction, color, true, FrameTiming.Update);
    }

    public static UniTask<DirectLight> Create(LightManager manager, Vector3 direction, Color4 color, bool useShadowMap)
    {
        return Create(manager, direction, color, useShadowMap, FrameTiming.Update);
    }

    public static async UniTask<DirectLight> Create(LightManager manager, Vector3 direction, Color4 color, bool useShadowMap, FrameTiming timing)
    {
        ArgumentNullException.ThrowIfNull(manager);

        var screen = manager.Screen;
        if(timing == FrameTiming.NotSpecified) {
            timing = FrameTiming.Update;
        }
        var timings = screen.Timings;
        await timings.FrameInitializing.NextOrNow();
        if(manager.TryRegisterLight(static (m, i, t) => new DirectLight(m, i, t), out var light) == false) {
            throw new InvalidOperationException("Cannot create light");
        }
        Debug.Assert(light.LifeState == LifeState.New);
        light._state = LifeState.Alive;
        light.Direction = direction;
        light.Color = color;
        if(useShadowMap) {
            manager.InitializeShadowMap(((ILight)light).Index, DefaultShadowMapSize);
        }
        var pipeline = screen.RenderPipeline;
        if(pipeline.TryFindOperation<ForwardRenderLayer>(RenderPipeline.DebuggerLayerName, out var debuggerLayer)) {
            await new DirectLightDebugObject(light).Activate(debuggerLayer);
        }
        await timings.GetTiming(timing).NextOrNow();
        return light;
    }
}

internal sealed class DirectLightDebugObject : Renderable
{
    private readonly DirectLight _light;
    public DirectLight TargetLight => _light;

    public DirectLightDebugObject(DirectLight light)
    {
        HasShadow = false;
        _light = light;
        light.Terminating.Subscribe((_, _) =>
        {
            this.Terminate().Forget();
            return UniTask.CompletedTask;
        });
        Activating.Subscribe(static (f, ct) =>
        {
            var self = SafeCast.As<DirectLightDebugObject>(f);
            self.OnActivating();
            return UniTask.CompletedTask;
        });
        Shader = new DirectLightDebugShader<VertexPosOnly>();
    }

    private void OnActivating()
    {
        ReadOnlySpan<VertexPosOnly> vertices = stackalloc VertexPosOnly[24]
        {
            new(-1f, 1f, -1f),
            new(-1f, 1f, 1f),
            new(1f, 1f, 1f),
            new(1f, 1f, -1f),
            new(-1f, 1f, -1f),
            new(-1f, -1f, -1f),
            new(-1f, -1f, 1f),
            new(-1f, 1f, 1f),
            new(-1f, 1f, 1f),
            new(-1f, -1f, 1f),
            new(1f, -1f, 1f),
            new(1f, 1f, 1f),
            new(1f, 1f, 1f),
            new(1f, -1f, 1f),
            new(1f, -1f, -1f),
            new(1f, 1f, -1f),
            new(1f, 1f, -1f),
            new(1f, -1f, -1f),
            new(-1f, -1f, -1f),
            new(-1f, 1f, -1f),
            new(-1f, -1f, 1f),
            new(-1f, -1f, -1f),
            new(1f, -1f, -1f),
            new(1f, -1f, 1f),
        };
        ReadOnlySpan<int> indices = stackalloc int[36] { 0, 1, 2, 0, 2, 3, 4, 5, 6, 4, 6, 7, 8, 9, 10, 8, 10, 11, 12, 13, 14, 12, 14, 15, 16, 17, 18, 16, 18, 19, 20, 21, 22, 20, 22, 23 };
        LoadMesh(vertices, indices);
    }

    protected override void OnRendering(in RenderingContext context)
    {
        var lightMat = _light.LightMatrix.DereferOrDefault();
        var mat = lightMat.Inverted();
        var newContext = new RenderingContext(
            context.Screen,
            context.Layer,
            context.Target,
            in mat,
            in context.View,
            in context.Projection);
        base.OnRendering(in newContext);
    }
}

internal sealed class DirectLightDebugShader<TVertex> : SingleTargetRenderingShader where TVertex : unmanaged, IVertex
{
    private float _width = 2;
    public float Width
    {
        get => _width;
        set => _width = MathF.Max(0, value);
    }

    public DirectLightDebugShader()
    {
    }

    protected override void DefineLocation(VertexDefinition definition, in LocationDefinitionContext context)
    {
        definition.Map<TVertex>("_pos", VertexFieldSemantics.Position);
    }

    protected override void OnRendering(ShaderDataDispatcher dispatcher, in RenderingContext context)
    {
        var mat = context.Projection * context.View * context.Model;
        var resolution = (Vector2)context.Screen.FrameBufferSize;
        dispatcher.SendUniform("_mat", mat);
        dispatcher.SendUniform("_resolutionInv", Vector2.One / resolution);
        dispatcher.SendUniform("_width", _width);
    }

    protected override ShaderSource GetShaderSource(in ShaderGetterContext context) => context.Layer switch
    {
        DeferredRenderLayer => throw new NotSupportedException(),
        ForwardRenderLayer => ForwardRenderingSource(),
        _ => throw new NotSupportedException(),
    };

    protected override void OnTargetAttached(Renderable target) { }

    protected override void OnTargetDetached(Renderable detachedTarget) { }

    private static ShaderSource ForwardRenderingSource() => new()
    {
        OnlyContainsConstLiteralUtf8 = true,
        VertexShader =
        """
        #version 410
        in vec3 _pos;
        uniform mat4 _mat;
        void main()
        {
            vec4 p = _mat * vec4(_pos, 1);
            gl_Position = vec4(p.xyz / p.w, 1);
        }
        """u8,
        GeometryShader =
        """
        #version 460
        uniform vec2 _resolutionInv;
        uniform float _width;
        layout (triangles) in;
        layout (triangle_strip, max_vertices = 16) out;
        void main()
        {
            vec2 s = _width * _resolutionInv;
            vec2 v0 = gl_in[1].gl_Position.xy - gl_in[0].gl_Position.xy;
            vec2 v1 = gl_in[2].gl_Position.xy - gl_in[1].gl_Position.xy;
            vec2 v2 = gl_in[0].gl_Position.xy - gl_in[2].gl_Position.xy;
            vec2 d0 = normalize(vec2(-v0.y, v0.x)) * s;
            vec2 d1 = normalize(vec2(-v1.y, v1.x)) * s;
            vec2 d2 = normalize(vec2(-v2.y, v2.x)) * s;

            // [0] -> [1]
            gl_Position = vec4(gl_in[0].gl_Position.xy - d0, gl_in[0].gl_Position.zw);
            EmitVertex();
            gl_Position = vec4(gl_in[1].gl_Position.xy - d0, gl_in[1].gl_Position.zw);
            EmitVertex();
            gl_Position = vec4(gl_in[0].gl_Position.xy + d0, gl_in[0].gl_Position.zw);
            EmitVertex();
            gl_Position = vec4(gl_in[1].gl_Position.xy + d0, gl_in[1].gl_Position.zw);
            EmitVertex();

            // [1] -> [2]
            gl_Position = vec4(gl_in[1].gl_Position.xy + d0, gl_in[1].gl_Position.zw);
            EmitVertex();
            gl_Position = vec4(gl_in[1].gl_Position.xy - d1, gl_in[1].gl_Position.zw);
            EmitVertex();

            gl_Position = vec4(gl_in[1].gl_Position.xy - d1, gl_in[1].gl_Position.zw);
            EmitVertex();
            gl_Position = vec4(gl_in[2].gl_Position.xy - d1, gl_in[2].gl_Position.zw);
            EmitVertex();
            gl_Position = vec4(gl_in[1].gl_Position.xy + d1, gl_in[1].gl_Position.zw);
            EmitVertex();
            gl_Position = vec4(gl_in[2].gl_Position.xy + d1, gl_in[2].gl_Position.zw);
            EmitVertex();

            gl_Position = vec4(gl_in[2].gl_Position.xy + d1, gl_in[2].gl_Position.zw);
            EmitVertex();
            gl_Position = vec4(gl_in[2].gl_Position.xy - d2, gl_in[2].gl_Position.zw);
            EmitVertex();

            // [2] -> [0]
            gl_Position = vec4(gl_in[2].gl_Position.xy - d2, gl_in[2].gl_Position.zw);
            EmitVertex();
            gl_Position = vec4(gl_in[0].gl_Position.xy - d2, gl_in[0].gl_Position.zw);
            EmitVertex();
            gl_Position = vec4(gl_in[2].gl_Position.xy + d2, gl_in[2].gl_Position.zw);
            EmitVertex();
            gl_Position = vec4(gl_in[0].gl_Position.xy + d2, gl_in[0].gl_Position.zw);
            EmitVertex();
        }
        """u8,
        FragmentShader =
        """
        #version 410
        out vec4 _outColor;
        void main()
        {
            _outColor = vec4(0, 0.5, 1, 1);
        }
        """u8,
    };
}

