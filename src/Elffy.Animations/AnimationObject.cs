#nullable enable
using Cysharp.Threading.Tasks;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Elffy
{
    [Obsolete("Don't use the class. Not implemented yet.", true)]
    public static class Animation
    {
        public static unsafe AnimationObject Linear => new AnimationObject(&PredefinedAnimations.Linear);
    }

    internal static class PredefinedAnimations
    {
        public static float Linear(float x) => x;
    }

    [Obsolete("Don't use the class. Not implemented yet.", true)]
    public readonly struct AnimationObject
    {
        private const int DummyFuncPointer = 1;

        private readonly IntPtr _func;
        private readonly object? _obj;    // IAnimationCurve or Func<float, float>

        [Obsolete("Don't use defaut constructor.", true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public AnimationObject() => throw new NotSupportedException("Don't use defaut constructor.");

        internal AnimationObject(IAnimationCurve curve)
        {
            ArgumentNullException.ThrowIfNull(nameof(curve));
            _func = IntPtr.Zero;
            _obj = curve;
        }

        internal unsafe AnimationObject(delegate*<float, float> func)
        {
            Debug.Assert(func != null);
            _func = (IntPtr)func;
            _obj = null;
        }

        internal AnimationObject(Func<float, float> func)
        {
            ArgumentNullException.ThrowIfNull(func);
            _func = (IntPtr)DummyFuncPointer;
            _obj = func;
        }

        public UniTask DuringTime<TState>(float millisecond, TState state, Action<TState, float> action, AnimationToken token)
        {
            return DuringTime(TimeSpanF.FromMilliseconds(millisecond), state, action, token);
        }

        public async UniTask DuringTime<TState>(TimeSpanF time, TState state, Action<TState, float> action, AnimationToken token)
        {
            var screen = token.Screen;
            var timingPoint = screen.Timings.Update;
            var startTime = screen.Time;
            while(true) {
                var t = screen.Time - startTime;
                if(t > time) { break; }
                action.Invoke(state, GetCurveValue(t / time));
                await timingPoint.Next();
            }
            //action.Invoke(state, GetCurveValue(1f));
        }

        private unsafe float GetCurveValue(float x)
        {
            var obj = _obj;
            if(obj is null) {
                var func = (delegate*<float, float>)_func;
                return func(x);
            }
            else if(_func == (IntPtr)DummyFuncPointer) {
                Debug.Assert(obj is not null);
                Debug.Assert(obj is Func<float, float>);
                var func = Unsafe.As<Func<float, float>>(obj);
                return func.Invoke(x);
            }
            else {
                Debug.Assert(obj is IAnimationCurve);
                var curve = Unsafe.As<IAnimationCurve>(obj);
                return curve.Curve(x);
            }
        }
    }

    public interface IAnimationCurve
    {
        float Curve(float x);
    }

    public readonly struct AnimationToken
    {
        private readonly AnimationTokenInfo _info;

        public IHostScreen Screen => _info.Screen;

        [Obsolete("Don't use defaut constructor.", true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public AnimationToken() => throw new NotSupportedException("Don't use defaut constructor.");

        private AnimationToken(AnimationTokenInfo info)
        {
            _info = info;
        }

        public static AnimationToken Create(IHostScreen screen)
        {
            return new AnimationToken(new AnimationTokenInfo(screen));
        }

        private sealed class AnimationTokenInfo
        {
            private IHostScreen _screen;

            public IHostScreen Screen => _screen;

            public AnimationTokenInfo(IHostScreen screen)
            {
                _screen = screen;
            }
        }
    }
}
