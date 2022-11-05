#nullable enable
using Cysharp.Threading.Tasks;
using Elffy.Shading;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Elffy;

public sealed class DirectLight : ILight
{
    private const int DefaultShadowMapSize = 2048;

    private LightManager? _manager;
    private LifeState _state;
    private Vector4 _position;
    private Color4 _color;
    private Matrix4 _lightMatrix;
    private ShadowMapData _shadowMap;
    private int _shadowMapSize; // should be power of 2.
    private AsyncEventSource<DirectLight>? _terminating;
    private EventSource<DirectLight>? _alive;
    private EventSource<DirectLight>? _dead;

    public Event<DirectLight> Alive => new(ref _alive);
    public AsyncEvent<DirectLight> Terminating => new(ref _terminating);
    public Event<DirectLight> Dead => new(ref _dead);

    public LifeState LifeState => _state;

    Vector4 ILight.Position
    {
        get => _position;
        set
        {
            if(value.W != 0) {
                throw new ArgumentException("W element must be 0.");
            }
            var dir = (-value.Xyz).Normalized();
            if(dir.ContainsNaNOrInfinity) {
                throw new ArgumentException("XYZ is zero vector or too small.");
            }
            var lightMatrix = CalcLightMatrix(in dir, Vector3.Zero, 30, 15, 20);  // TODO: 
            _position = value;
            _lightMatrix = lightMatrix;
        }
    }

    public Vector3 Direction
    {
        get
        {
            if(_position.W == 0) {
                var dir = (-_position.Xyz).Normalized();
                if(dir.ContainsNaNOrInfinity == false) {
                    return dir;
                }
            }
            // invalid
            return Vector3.Zero;
        }
        set => ((ILight)this).Position = new Vector4(-value, 0);
    }

    public Color4 Color
    {
        get => _color;
        set => _color = value;
    }

    public int ShadowMapSize
    {
        get => _shadowMapSize;
        init
        {
            if(value <= 0) {
                ThrowOutOfRange();
                [DoesNotReturn] static void ThrowOutOfRange() => throw new ArgumentOutOfRangeException(nameof(value));
            }
            if(BitOperations.IsPow2(value) == false) {
                ThrowNotPowerOf2();
                [DoesNotReturn] static void ThrowNotPowerOf2() => throw new ArgumentException("The value should be power of 2.");
            }
            _shadowMapSize = value;
        }
    }

    public RefReadOnly<Matrix4> LightMatrix => new(in _lightMatrix);

    public RefReadOnly<ShadowMapData> ShadowMap => new(in _shadowMap);

    public DirectLight()
    {
        _state = LifeState.New;
        _shadowMapSize = DefaultShadowMapSize;
        Color = Color4.White;
        Direction = new Vector3(0, -1, 0);
    }

    public UniTask<DirectLight> Activate(LightManager manager, CancellationToken cancellationToken = default)
        => Activate(manager, FrameTiming.Update, cancellationToken);

    public UniTask<DirectLight> Activate(LightManager manager, FrameTiming timing, CancellationToken cancellationToken = default)
    {
        var timings = manager.Screen.Timings;
        var tp = timings.GetTiming(timing.IsSpecified() ? timing : FrameTiming.Update);
        CheckForActivation(manager, tp);
        return ActivatePrivate(manager, tp, cancellationToken);
    }

    public UniTask<DirectLight> Activate(LightManager manager, FrameTimingPoint timingPoint, CancellationToken cancellationToken = default)
    {
        CheckForActivation(manager, timingPoint);
        return ActivatePrivate(manager, timingPoint, cancellationToken);
    }

    private void CheckForActivation(LightManager manager, FrameTimingPoint timingPoint)
    {
        ArgumentNullException.ThrowIfNull(manager);
        ArgumentNullException.ThrowIfNull(timingPoint);
        var currentContext = Engine.CurrentContext;
        if(currentContext == null) {
            ContextMismatchException.ThrowCurrentContextIsNull();
        }
        if(manager.Screen != currentContext) {
            throw new ArgumentException($"'{nameof(manager)}' instance does not belong in the current context.");
        }
        if(timingPoint.Screen != currentContext) {
            throw new ArgumentException($"'{nameof(timingPoint)}' instance does not belong in the current context.");
        }
        if(_state > LifeState.New) {
            throw new InvalidOperationException($"Cannot activate the instance twice.");
        }
    }

    private async UniTask<DirectLight> ActivatePrivate(LightManager manager, FrameTimingPoint timingPoint, CancellationToken ct)
    {
        Debug.Assert(_state == LifeState.New);
        _state = LifeState.Activating;
        _manager = manager;
        manager.AddLight(this, static self =>
        {
            SafeCast.As<DirectLight>(self).OnAddedToList();
        });
        _shadowMap.Initialize(_shadowMapSize, _shadowMapSize);
        var pipeline = manager.Screen.RenderPipeline;
        if(pipeline.TryFindDebuggerLayer(out var debuggerLayer)) {
            await new DirectLightDebugObject(this).Activate(debuggerLayer, ct);
        }
        await timingPoint.NextFrame(ct);
        return this;
    }

    public UniTask<DirectLight> Terminate(CancellationToken cancellationToken = default) => Terminate(FrameTiming.Update, cancellationToken);

    public UniTask<DirectLight> Terminate(FrameTiming timing, CancellationToken cancellationToken = default)
    {
        CheckForTermination(
            timing.IsSpecified() ? timing : FrameTiming.Update,
            out var timingPoint);
        return TerminatePrivate(_manager, timingPoint, cancellationToken);
    }

    public UniTask<DirectLight> Terminate(FrameTimingPoint timingPoint, CancellationToken cancellationToken = default)
    {
        CheckForTermination(timingPoint);
        return TerminatePrivate(_manager, timingPoint, cancellationToken);
    }

    [MemberNotNull(nameof(_manager))]
    private void CheckForTermination(FrameTimingPoint timingPoint)
    {
        ArgumentNullException.ThrowIfNull(timingPoint);
        var currentContext = Engine.CurrentContext;
        if(currentContext == null) {
            ContextMismatchException.ThrowCurrentContextIsNull();
        }
        if(_state >= LifeState.Terminating) {
            throw new InvalidOperationException($"Cannot terminate the instance twice.");
        }
        Debug.Assert(_manager is not null);
        var manager = _manager;
        if(manager.Screen != currentContext) {
            throw new ArgumentException($"'{nameof(manager)}' instance does not belong in the current context.");
        }
        if(timingPoint.Screen != currentContext) {
            throw new ArgumentException($"'{nameof(timingPoint)}' instance does not belong in the current context.");
        }
    }

    [MemberNotNull(nameof(_manager))]
    private void CheckForTermination(FrameTiming timing, out FrameTimingPoint timingPoint)
    {
        var currentContext = Engine.CurrentContext;
        if(currentContext == null) {
            ContextMismatchException.ThrowCurrentContextIsNull();
        }
        if(_state >= LifeState.Terminating) {
            throw new InvalidOperationException($"Cannot terminate the instance twice.");
        }
        Debug.Assert(_manager is not null);
        var manager = _manager;
        if(manager.Screen != currentContext) {
            throw new ArgumentException($"'{nameof(manager)}' instance does not belong in the current context.");
        }
        Debug.Assert(timing.IsSpecified());
        timingPoint = currentContext.Timings.GetTiming(timing);
    }

    private async UniTask<DirectLight> TerminatePrivate(LightManager manager, FrameTimingPoint timingPoint, CancellationToken ct)
    {
        var screen = manager.Screen;
        screen.ThrowIfCurrentTimingOufOfFrame();

        var timings = screen.Timings;
        Debug.Assert(_state == LifeState.Alive);
        _state = LifeState.Terminating;
        manager.RemoveLight(this, static self =>
        {
            SafeCast.As<DirectLight>(self).OnRemovedFromList();
        });
        try {
            await _terminating.InvokeIfNotNull(this, CancellationToken.None);
        }
        catch {
            if(EngineSetting.UserCodeExceptionCatchMode == UserCodeExceptionCatchMode.Throw) { throw; }
        }
        try {
            await timings.FrameFinalizing.NextOrNow(ct);
        }
        catch {
            if(EngineSetting.UserCodeExceptionCatchMode == UserCodeExceptionCatchMode.Throw) { throw; }
        }
        await timingPoint.NextFrame(ct);
        return this;
    }

    private void OnAddedToList()
    {
        Debug.Assert(_state == LifeState.Activating);
        _state = LifeState.Alive;
        try {
            _alive?.Invoke(this);
        }
        catch {
            if(EngineSetting.UserCodeExceptionCatchMode == UserCodeExceptionCatchMode.Throw) { throw; }
        }
    }

    private void OnRemovedFromList()
    {
        Debug.Assert(_state == LifeState.Terminating);
        _state = LifeState.Dead;
        _shadowMap.Release();
        try {
            _dead?.Invoke(this);
        }
        catch {
            if(EngineSetting.UserCodeExceptionCatchMode == UserCodeExceptionCatchMode.Throw) { throw; }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IHostScreen GetValidScreen()
    {
        var manager = _manager;
        if(manager is null) { ThrowHelper.ThrowInvalidNullScreen(); }
        return manager.Screen;
    }

    private static Matrix4 CalcLightMatrix(in Vector3 directionNormalized, in Vector3 target, float frontLen, float backLen, float width)
    {
        Debug.Assert(frontLen > 0);
        Debug.Assert(backLen > 0);
        var halfWidth = width * 0.5f;
        var origin = target - frontLen * directionNormalized;
        var dirX0Z = new Vector3(directionNormalized.X, 0, directionNormalized.Z).Normalized();
        Vector3 up;
        if(dirX0Z.ContainsNaNOrInfinity) {
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
        ref readonly var lightMat = ref _light.LightMatrix.GetReference();
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

