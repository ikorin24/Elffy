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
        set
        {
            if(value.W != 0) {
                throw new ArgumentException("W element must be 0.");
            }
            var dir = (-value.Xyz).Normalized();
            if(dir.IsInvalid) {
                throw new ArgumentException("XYZ is zero vector or too small.");
            }
            var lightMatrix = CalcLightMatrix(in dir, Vector3.Zero, 30, 15, 20);  // TODO: 
            _impl.TrySetPosition(in value, in lightMatrix);
        }
    }

    public Vector3 Direction
    {
        get
        {
            if(_impl.GetPosition().TryDerefer(out var pos)) {
                if(pos.W == 0) {
                    return -pos.Xyz;
                }
            }
            return Vector3.Zero;
        }
        set => ((ILight)this).Position = new Vector4(-value, 0);
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

    private static Matrix4 CalcLightMatrix(in Vector3 directionNormalized, in Vector3 target, float frontLen, float backLen, float width)
    {
        Debug.Assert(frontLen > 0);
        Debug.Assert(backLen > 0);
        var halfWidth = width * 0.5f;
        var origin = target - frontLen * directionNormalized;
        var dirX0Z = new Vector3(directionNormalized.X, 0, directionNormalized.Z).Normalized();
        Vector3 up;
        if(dirX0Z.IsInvalid) {
            // direction is (0, +-1, 0) or very close to them.
            up = Vector3.UnitX;
        }
        else {
            up = Quaternion.FromTwoVectors(dirX0Z, directionNormalized) * Vector3.UnitY;
        }
        const float Near = 0;
        float far = frontLen + backLen;
        Matrix4.LookAt(origin, target, up, out var lightView);
        Matrix4.OrthographicProjection(-halfWidth, halfWidth, -halfWidth, halfWidth, Near, far, out var lightProjection);
        return lightProjection * lightView;
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
        Shader = new DirectLightDebugShader<VertexPosOnly>
        {
            LineWidth = 3f,
        };
    }

    private void OnActivating()
    {
        ReadOnlySpan<VertexPosOnly> vertices = stackalloc VertexPosOnly[8]
        {
            new(-1f, 1f, 1f),
            new(-1f, -1f, 1f),
            new(1f, -1f, 1f),
            new(1f, 1f, 1f),
            new(-1f, 1f, -1f),
            new(-1f, -1f, -1f),
            new(1f, -1f, -1f),
            new(1f, 1f, -1f),
        };

        // 0   0 -- 3
        // | \  \   |
        // |  \  \  |
        // |   \  \ |
        // 1 -- 2   2
        // 
        // 0 -- 1 and 1 -- 2 are edges of a box.
        // 2 -- 0 is a diagonal line of a box.
        ReadOnlySpan<int> indices = stackalloc int[] { 0, 1, 2, 2, 3, 0, 7, 6, 5, 5, 4, 7, 4, 0, 3, 3, 7, 4, 1, 5, 6, 6, 2, 1, 4, 5, 1, 1, 0, 4, 3, 2, 6, 6, 7, 3 };
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
    private float _lineWidth = 2;
    private Color4 _lineColor = new Color4(0, 0.5f, 1f, 1f);
    public float LineWidth
    {
        get => _lineWidth;
        set => _lineWidth = MathF.Max(0, value);
    }

    public Color4 LineColor
    {
        get => _lineColor;
        set => _lineColor = value;
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
        var resolution = context.Screen.FrameBufferSize;
        dispatcher.SendUniform("_mat", mat);
        dispatcher.SendUniform("_resolutionInv", Vector2.One / resolution.ToVector2());
        dispatcher.SendUniform("_lineWidth", _lineWidth);
        dispatcher.SendUniform("_lineColor", _lineColor);
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
            // Set gl_Position.w to 1 for easy handling in the geometry shader.
            vec4 p = _mat * vec4(_pos, 1);
            gl_Position = vec4(p.xyz / p.w, 1);
        }
        """u8,
        GeometryShader =
        """
        #version 460
        uniform vec2 _resolutionInv;
        uniform float _lineWidth;
        layout (triangles) in;
        layout (triangle_strip, max_vertices = 16) out;
        void main()
        {
            vec2 s = _lineWidth * _resolutionInv;       // (_lineWidth * 0.5) * (_resolutionInv * 2)
            vec2 v0 = gl_in[1].gl_Position.xy - gl_in[0].gl_Position.xy;
            vec2 v1 = gl_in[2].gl_Position.xy - gl_in[1].gl_Position.xy;
            vec2 d0 = normalize(vec2(-v0.y, v0.x)) * s;
            vec2 d1 = normalize(vec2(-v1.y, v1.x)) * s;

            // Line from [0] to [1]
            gl_Position = vec4(gl_in[0].gl_Position.xy - d0, gl_in[0].gl_Position.zw);
            EmitVertex();
            gl_Position = vec4(gl_in[1].gl_Position.xy - d0, gl_in[1].gl_Position.zw);
            EmitVertex();
            gl_Position = vec4(gl_in[0].gl_Position.xy + d0, gl_in[0].gl_Position.zw);
            EmitVertex();
            gl_Position = vec4(gl_in[1].gl_Position.xy + d0, gl_in[1].gl_Position.zw);
            EmitVertex();

            // dummy
            gl_Position = vec4(gl_in[1].gl_Position.xy + d0, gl_in[1].gl_Position.zw);
            EmitVertex();
            gl_Position = vec4(gl_in[1].gl_Position.xy - d1, gl_in[1].gl_Position.zw);
            EmitVertex();

            // Line from [1] to [2]
            gl_Position = vec4(gl_in[1].gl_Position.xy - d1, gl_in[1].gl_Position.zw);
            EmitVertex();
            gl_Position = vec4(gl_in[2].gl_Position.xy - d1, gl_in[2].gl_Position.zw);
            EmitVertex();
            gl_Position = vec4(gl_in[1].gl_Position.xy + d1, gl_in[1].gl_Position.zw);
            EmitVertex();
            gl_Position = vec4(gl_in[2].gl_Position.xy + d1, gl_in[2].gl_Position.zw);
            EmitVertex();

            // Line from [2] to [0] is diagonal line.
        }
        """u8,
        FragmentShader =
        """
        #version 410
        uniform vec4 _lineColor;
        out vec4 _outColor;
        void main()
        {
            _outColor = _lineColor;
        }
        """u8,
    };
}

