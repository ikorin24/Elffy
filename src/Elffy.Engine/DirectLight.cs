#nullable enable
using Cysharp.Threading.Tasks;
using Elffy.Effective;
using Elffy.Shading;
using Elffy.Threading;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace Elffy;

public sealed class DirectLight : ILight, IFramedLifetime<DirectLight>
{
    private RenderPipeline? _pipeline;
    private readonly CascadedShadowMap _shadowMap;
    private DirectLightMatrixCalcFunc? _lightMatrixCalculator;
    private LifeState _state;
    private Vector4 _position;
    private Color4 _color;
    private AsyncEventSource<DirectLight> _activating;
    private AsyncEventSource<DirectLight> _terminating;
    private EventSource<DirectLight> _alive;
    private EventSource<DirectLight> _dead;
    private readonly SubscriptionBag _subscriptions = new SubscriptionBag();

    public IHostScreen? Screen => _pipeline?.Screen;

    public AsyncEvent<DirectLight> Activating => _activating.Event;
    public Event<DirectLight> Alive => _alive.Event;
    public AsyncEvent<DirectLight> Terminating => _terminating.Event;
    public Event<DirectLight> Dead => _dead.Event;

    public LifeState LifeState => _state;

    Vector4 ILight.Position
    {
        get => _position;
        set
        {
            if(TryGetDirection(value, out var dir, out var error) == false) {
                throw new ArgumentException(error);
            }
            if(_state.IsRunning()) {
                UpdateLightMatrices(in dir);
            }
            _position = value;
        }
    }

    public Vector3 Direction
    {
        get => TryGetDirection(_position, out var dir, out _) ? dir : Vector3.Zero;
        set => ((ILight)this).Position = new Vector4(-value, 0);
    }

    public Color4 Color
    {
        get => _color;
        set => _color = value;
    }

    public CascadedShadowMap ShadowMap => _shadowMap;

    public SubscriptionRegister Subscriptions => _subscriptions.Register;

    public DirectLight()
    {
        _state = LifeState.New;
        _shadowMap = new CascadedShadowMap(this);
        Color = Color4.White;
        Direction = new Vector3(0, -1, 0);
    }

    public UniTask<DirectLight> Activate(RenderPipeline pipeline, DirectLightConfig config, CancellationToken cancellationToken = default)
        => Activate(pipeline, config, FrameTiming.Update, cancellationToken);

    public UniTask<DirectLight> Activate(RenderPipeline pipeline, DirectLightConfig config, FrameTiming timing, CancellationToken cancellationToken = default)
    {
        var timings = pipeline.Screen.Timings;
        var tp = timings.GetTiming(timing.IsSpecified() ? timing : FrameTiming.Update);
        CheckForActivation(pipeline, config, tp);
        return ActivatePrivate(pipeline, config, tp, cancellationToken);
    }

    public UniTask<DirectLight> Activate(RenderPipeline pipeline, DirectLightConfig config, FrameTimingPoint timingPoint, CancellationToken cancellationToken = default)
    {
        CheckForActivation(pipeline, config, timingPoint);
        return ActivatePrivate(pipeline, config, timingPoint, cancellationToken);
    }

    private void CheckForActivation(RenderPipeline pipeline, DirectLightConfig config, FrameTimingPoint timingPoint)
    {
        ArgumentNullException.ThrowIfNull(pipeline);
        ArgumentNullException.ThrowIfNull(timingPoint);
        var currentContext = Engine.CurrentContext;
        if(currentContext == null) {
            ContextMismatchException.ThrowCurrentContextIsNull();
        }
        if(pipeline.Screen != currentContext) {
            throw new ArgumentException($"'{nameof(pipeline)}' instance does not belong in the current context.");
        }
        if(timingPoint.Screen != currentContext) {
            throw new ArgumentException($"'{nameof(timingPoint)}' instance does not belong in the current context.");
        }
        if(_state > LifeState.New) {
            throw new InvalidOperationException($"Cannot activate the instance twice.");
        }
        config.ValidateArg();
    }

    private async UniTask<DirectLight> ActivatePrivate(RenderPipeline pipeline, DirectLightConfig config, FrameTimingPoint timingPoint, CancellationToken ct)
    {
        Debug.Assert(_state == LifeState.New);
        _state = LifeState.Activating;
        _pipeline = pipeline;

        ExceptionDispatchInfo? edi = null;
        try {
            await _activating.Invoke(this, ct);
            _activating.Clear();
        }
        catch(Exception ex) {
            if(EngineSetting.UserCodeExceptionCatchMode == UserCodeExceptionCatchMode.Throw) {
                edi = ExceptionDispatchInfo.Capture(ex);
            }
        }

        pipeline.AddLight(this, static self =>
        {
            SafeCast.As<DirectLight>(self).OnAddedToList();
        });
        _lightMatrixCalculator = config.LightMatrixCalculator ?? DirectLightMatrixCalculator.DefaultFunc;
        _shadowMap.Initialize(config);
        UpdateLightMatrices(Direction);

        // [capture] this
        pipeline.Screen.Camera.MatrixChanged.Subscribe(camera =>
        {
            UpdateLightMatrices(camera.Direction);
        }).AddTo(_subscriptions);

        if(pipeline.TryFindDebuggerLayer(out var debuggerLayer)) {
            using var tasks = new ParallelOperation();
            for(int i = 0; i < config.CascadeCount; i++) {
                var debugObject = new DirectLightDebugObject(this, i);
                var activation = debugObject.Activate(debuggerLayer, timingPoint, ct);
                tasks.Add(activation);
            }
            await tasks.WhenAll();
        }
        await timingPoint.NextOrNow(ct);
        if(EngineSetting.UserCodeExceptionCatchMode == UserCodeExceptionCatchMode.Throw) {
            edi?.Throw();
        }
        return this;
    }

    public UniTask<DirectLight> Terminate(CancellationToken cancellationToken = default) => Terminate(FrameTiming.Update, cancellationToken);

    public UniTask<DirectLight> Terminate(FrameTiming timing, CancellationToken cancellationToken = default)
    {
        CheckForTermination(
            timing.IsSpecified() ? timing : FrameTiming.Update,
            out var timingPoint);
        return TerminatePrivate(_pipeline, timingPoint, cancellationToken);
    }

    public UniTask<DirectLight> Terminate(FrameTimingPoint timingPoint, CancellationToken cancellationToken = default)
    {
        CheckForTermination(timingPoint);
        return TerminatePrivate(_pipeline, timingPoint, cancellationToken);
    }

    [MemberNotNull(nameof(_pipeline))]
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
        Debug.Assert(_pipeline is not null);
        var pipeline = _pipeline;
        if(pipeline.Screen != currentContext) {
            throw new ArgumentException($"'{nameof(pipeline)}' instance does not belong in the current context.");
        }
        if(timingPoint.Screen != currentContext) {
            throw new ArgumentException($"'{nameof(timingPoint)}' instance does not belong in the current context.");
        }
    }

    [MemberNotNull(nameof(_pipeline))]
    private void CheckForTermination(FrameTiming timing, out FrameTimingPoint timingPoint)
    {
        var currentContext = Engine.CurrentContext;
        if(currentContext == null) {
            ContextMismatchException.ThrowCurrentContextIsNull();
        }
        if(_state >= LifeState.Terminating) {
            throw new InvalidOperationException($"Cannot terminate the instance twice.");
        }
        Debug.Assert(_pipeline is not null);
        var pipeline = _pipeline;
        if(pipeline.Screen != currentContext) {
            throw new ArgumentException($"'{nameof(pipeline)}' instance does not belong in the current context.");
        }
        Debug.Assert(timing.IsSpecified());
        timingPoint = currentContext.Timings.GetTiming(timing);
    }

    private async UniTask<DirectLight> TerminatePrivate(RenderPipeline pipeline, FrameTimingPoint timingPoint, CancellationToken ct)
    {
        var screen = pipeline.Screen;
        screen.ThrowIfCurrentTimingOufOfFrame();

        var timings = screen.Timings;
        Debug.Assert(_state == LifeState.Alive);
        _state = LifeState.Terminating;
        pipeline.RemoveLight(this, static self =>
        {
            SafeCast.As<DirectLight>(self).OnRemovedFromList();
        });
        try {
            await _terminating.Invoke(this, CancellationToken.None);
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
            _alive.Invoke(this);
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
        _subscriptions.Dispose();
        try {
            _dead.Invoke(this);
        }
        catch {
            if(EngineSetting.UserCodeExceptionCatchMode == UserCodeExceptionCatchMode.Throw) { throw; }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IHostScreen GetValidScreen()
    {
        var pipeline = _pipeline;
        if(pipeline is null) { ThrowHelper.ThrowInvalidNullScreen(); }
        return pipeline.Screen;
    }

    [SkipLocalsInit]
    private void UpdateLightMatrices(in Vector3 direction)
    {
        const int Threshold = 16;

        var lightMatrixCalculator = _lightMatrixCalculator;
        Debug.Assert(lightMatrixCalculator != null);
        var count = ShadowMap.CascadeCount;
        using var memory = count <= Threshold ? default : new ValueTypeRentMemory<Matrix4>(count, false);
        Span<Matrix4> matrices = count <= Threshold ? (stackalloc Matrix4[Threshold])[..count] : memory.AsSpan();
        for(int i = 0; i < matrices.Length; i++) {
            matrices[i] = lightMatrixCalculator.Invoke(i, this);
        }
        _shadowMap.UpdateLightMatrices(matrices);
    }

    private static bool TryGetDirection(in Vector4 position4, out Vector3 dir, [MaybeNullWhen(true)] out string error)
    {
        if(position4.W == 0) {
            dir = (-position4.Xyz).Normalized();
            if(dir.ContainsNaNOrInfinity == false) {
                error = null;
                return true;
            }
            error = "W element must be 0.";
        }
        else {
            error = "XYZ is zero vector or too small.";
        }
        dir = default;
        return false;
    }

    public bool TryGetScreen([MaybeNullWhen(false)] out IHostScreen screen)
    {
        throw new NotImplementedException();
    }
}

internal static class DirectLightMatrixCalculator
{
    public static DirectLightMatrixCalcFunc DefaultFunc = (int cascade, DirectLight light) =>
    {
        var screen = light.GetValidScreen();
        var subfrustum = CalcSubfrustum(cascade, screen.Camera, light);
        var (worldToLight, lightOriginInWorld) = CalcWorldToLightSpace(subfrustum, light.Direction);
        var aabbInLight = (Min: Vector3.MaxValue, Max: Vector3.MinValue);
        foreach(var corner in subfrustum.Corners) {
            var p = worldToLight.TransformFast4x3(corner);
            aabbInLight.Min = Vector3.Min(p, aabbInLight.Min);
            aabbInLight.Max = Vector3.Max(p, aabbInLight.Max);
        }
        const float ZScale = 4f;       // TODO:

        Matrix4.OrthographicProjection(
            aabbInLight.Min.X, aabbInLight.Max.X,
            aabbInLight.Min.Y, aabbInLight.Max.Y,
            aabbInLight.Min.Z * ZScale, aabbInLight.Max.Z * ZScale,
            out var projection);
        return projection * worldToLight;
    };

    private static Frustum CalcSubfrustum(int cascade, Camera camera, DirectLight light)
    {
        var frustum = camera.Frustum;
        var cascadeCount = light.ShadowMap.CascadeCount;
        const float MaxShadowNearToFar = 200f;       // TODO:
        var cameraNearToFar = camera.Far - camera.Near;
        var shadowNearToFar = float.Min(cameraNearToFar, MaxShadowNearToFar);
        var coeff = shadowNearToFar / cameraNearToFar;
        var nearPoint = cascade / (float)cascadeCount * coeff;
        var farPoint = (cascade + 1) / (float)cascadeCount * coeff;
        var subfrustum = new Frustum
        {
            NearLeftBottom = Vector3.Mix(frustum.NearLeftBottom, frustum.FarLeftBottom, nearPoint),
            NearLeftTop = Vector3.Mix(frustum.NearLeftTop, frustum.FarLeftTop, nearPoint),
            NearRightBottom = Vector3.Mix(frustum.NearRightBottom, frustum.FarRightBottom, nearPoint),
            NearRightTop = Vector3.Mix(frustum.NearRightTop, frustum.FarRightTop, nearPoint),
            FarLeftBottom = Vector3.Mix(frustum.NearLeftBottom, frustum.FarLeftBottom, farPoint),
            FarLeftTop = Vector3.Mix(frustum.NearLeftTop, frustum.FarLeftTop, farPoint),
            FarRightBottom = Vector3.Mix(frustum.NearRightBottom, frustum.FarRightBottom, farPoint),
            FarRightTop = Vector3.Mix(frustum.NearRightTop, frustum.FarRightTop, farPoint),
        };
        return subfrustum;
    }

    private static (Matrix4 WorldToLight, Vector3 OriginInWorldSpace) CalcWorldToLightSpace(in Frustum frustumInWorld, Vector3 lightDir)
    {
        var center = frustumInWorld.Center;
        var dirX0Z = new Vector3(lightDir.X, 0, lightDir.Z).Normalized();

        Vector3 up;
        if(dirX0Z.ContainsNaNOrInfinity) {
            // direction is (0, +-1, 0) or very close to them.
            up = Vector3.UnitX;
        }
        else {
            up = Quaternion.FromTwoVectors(dirX0Z, lightDir) * Vector3.UnitY;
        }
        var worldToLight = Matrix4.LookAt(center - lightDir, center, up);
        return (WorldToLight: worldToLight, OriginInWorldSpace: center);
    }
}

public record struct DirectLightConfig
{
    internal const int MaxCascadeCount = 5;

    public int ShadowMapSize { get; init; } = 1024;
    public int CascadeCount { get; init; } = 1;
    public DirectLightMatrixCalcFunc? LightMatrixCalculator { get; init; }

    public static DirectLightConfig Default => new();

    public DirectLightConfig()
    {
    }

    internal void ValidateArg()
    {
        if(CascadeCount <= 0) {
            throw new ArgumentOutOfRangeException(nameof(CascadeCount));
        }
        if(CascadeCount > MaxCascadeCount) {
            throw new ArgumentOutOfRangeException("Shadow map cascade count is too large.");
        }
        if(ShadowMapSize <= 0) {
            throw new ArgumentOutOfRangeException(nameof(ShadowMapSize));
        }
        if(BitOperations.IsPow2(ShadowMapSize) == false) {
            throw new ArgumentException("shadow map size should be power of 2.");
        }
    }
}

public delegate Matrix4 DirectLightMatrixCalcFunc(int cascade, DirectLight light);

internal sealed class DirectLightDebugObject : Renderable
{
    private readonly DirectLight _light;
    private readonly int _cascadeIndex;

    private static readonly Color4[] _colorTable = new Color4[7]
    {
        new Color4(1f, 0f, 0f, 1f),
        new Color4(1f, 0.5882353f, 0f, 1f),
        new Color4(1f, 0.9411765f, 0f, 1f),
        new Color4(0f, 0.5294118f, 0f, 1f),
        new Color4(0f, 0.5686275f, 1f, 1f),
        new Color4(0f, 0.39215687f, 0.74509805f, 1f),
        new Color4(0.5686275f, 0f, 0.50980395f, 1f),
    };

    public DirectLight TargetLight => _light;

    public DirectLightDebugObject(DirectLight light, int cascadeIndex)
    {
        _cascadeIndex = cascadeIndex;
        _light = light;
        HasShadow = false;
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

        var color = _colorTable[cascadeIndex % _colorTable.Length];
        Shader = new DirectLightDebugShader<VertexPosOnly>
        {
            LineWidth = 3f,
            LineColor = color,
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
        var shadowMap = _light.ShadowMap;
        ref readonly var lightMat = ref shadowMap.LightMatrices[_cascadeIndex];
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
        precision highp float;
        in vec3 _pos;
        uniform mat4 _mat;
        void main()
        {
            // Set gl_Position.w to 1 for easy handling in the geometry shader.
            gl_Position = _mat * vec4(_pos, 1.0);
        }
        """u8,
        GeometryShader =
        """
        #version 460
        precision highp float;
        uniform vec2 _resolutionInv;
        uniform float _lineWidth;
        layout (triangles) in;
        layout (triangle_strip, max_vertices = 10) out;
        void main()
        {
            vec2 s = _lineWidth * _resolutionInv;       // (_lineWidth * 0.5) * (_resolutionInv * 2)
            vec3[3] pos = vec3[](
                gl_in[0].gl_Position.xyz / gl_in[0].gl_Position.w,
                gl_in[1].gl_Position.xyz / gl_in[1].gl_Position.w,
                gl_in[2].gl_Position.xyz / gl_in[2].gl_Position.w
            );
            vec3 v0 = pos[1].xyz - pos[0].xyz;
            vec3 v1 = pos[2].xyz - pos[1].xyz;
            vec2 d0 = vec2(-v0.y, v0.x) / length(vec2(-v0.y, v0.x)) * s * sign(v0.z);
            vec2 d1 = vec2(-v1.y, v1.x) / length(vec2(-v1.y, v1.x)) * s * sign(v1.z);

            // Line from [0] to [1]
            gl_Position = vec4(vec3(pos[0].xy - d0, pos[0].z) * gl_in[0].gl_Position.w, gl_in[0].gl_Position.w); EmitVertex();
            gl_Position = vec4(vec3(pos[1].xy - d0, pos[1].z) * gl_in[1].gl_Position.w, gl_in[1].gl_Position.w); EmitVertex();
            gl_Position = vec4(vec3(pos[0].xy + d0, pos[0].z) * gl_in[0].gl_Position.w, gl_in[0].gl_Position.w); EmitVertex();
            gl_Position = vec4(vec3(pos[1].xy + d0, pos[1].z) * gl_in[1].gl_Position.w, gl_in[1].gl_Position.w); EmitVertex();

            // dummy
            gl_Position = vec4(vec3(pos[1].xy + d0, pos[1].z) * gl_in[1].gl_Position.w, gl_in[1].gl_Position.w); EmitVertex();
            gl_Position = vec4(vec3(pos[1].xy - d1, pos[1].z) * gl_in[1].gl_Position.w, gl_in[1].gl_Position.w); EmitVertex();

            // Line from [1] to [2]
            gl_Position = vec4(vec3(pos[1].xy - d1, pos[1].z) * gl_in[1].gl_Position.w, gl_in[1].gl_Position.w); EmitVertex();
            gl_Position = vec4(vec3(pos[2].xy - d1, pos[2].z) * gl_in[2].gl_Position.w, gl_in[2].gl_Position.w); EmitVertex();
            gl_Position = vec4(vec3(pos[1].xy + d1, pos[1].z) * gl_in[1].gl_Position.w, gl_in[1].gl_Position.w); EmitVertex();
            gl_Position = vec4(vec3(pos[2].xy + d1, pos[2].z) * gl_in[2].gl_Position.w, gl_in[2].gl_Position.w); EmitVertex();

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
