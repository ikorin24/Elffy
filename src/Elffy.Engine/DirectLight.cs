#nullable enable
using Cysharp.Threading.Tasks;
using Elffy.Effective.Unsafes;
using Elffy.Shading;
using Elffy.Shading.Forward;
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

    public bool HasShadowMap => _impl.GetShadowMapOrEmpty().IsEmpty == false;

    Vector4 ILight.Position
    {
        get => _impl.GetPositionOrZero();
        set => _impl.TrySetPosition(value);
    }

    public Vector3 Direction
    {
        get
        {
            ref readonly var pos = ref _impl.GetPositionOrNull();
            return UnsafeEx.IsNullRefReadOnly(in pos) ? default : pos.Xyz / pos.W;
        }
        set => _impl.TrySetPosition(new Vector4(-value, 1f));
    }

    public Color4 Color
    {
        get => _impl.GetColorOrZero();
        set => _impl.TrySetColor(value);
    }

    public ref readonly Matrix4 LightMatrix => ref _impl.GetLightMatrixOrZero();

    public ref readonly ShadowMapData ShadowMap => ref _impl.GetShadowMapOrEmpty();

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
        if(pipeline.TryGetOperation<ForwardRenderLayer>(RenderPipeline.DebuggerLayerName, out var debuggerLayer)) {
            await new DirectLightDebugObject(light).Activate(debuggerLayer);
        }
        await timings.GetTiming(timing).NextOrNow();
        return light;
    }
}

internal sealed class DirectLightDebugObject : Renderable
{
    private readonly DirectLight _light;

    public DirectLightDebugObject(DirectLight light)
    {
        _light = light;
        light.Terminating.Subscribe(async (_, _) =>
        {
            await this.Terminate(FrameTiming.FrameFinalizing);
        });
        Activating.Subscribe(static (f, ct) =>
        {
            PrimitiveMeshProvider<Vertex>.GetCube(
                SafeCast.As<DirectLightDebugObject>(f),
                static (self, vertices, indices) => self.LoadMesh(vertices, indices));
            return UniTask.CompletedTask;
        });
        Shader = new WireframeShader();
        //Shader = new PhongShader();
    }
}
