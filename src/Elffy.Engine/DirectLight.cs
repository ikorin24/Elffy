#nullable enable
using Cysharp.Threading.Tasks;
using Elffy.Effective.Unsafes;
using Elffy.Shading;
using System;
using System.Diagnostics;

namespace Elffy
{
    public sealed class DirectLight : ILight
    {
        private const int DefaultShadowMapSize = 2048;
        private LightImpl _impl;
        private LifeState _state;

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
            await timings.AfterRendering.NextOrNow();
            manager.RemoveLight(this);
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
            if(timing == FrameTiming.NotSpecified) {
                timing = FrameTiming.Update;
            }

            var timings = manager.Screen.Timings;
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
            await timings.GetTiming(timing).NextOrNow();
            return light;
        }
    }
}
