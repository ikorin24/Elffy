#nullable enable
using Elffy.Effective;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Elffy
{
    public sealed class LightManager
    {
        private record struct BufferItem(ILight Light, Action<ILight> Callback);

        private readonly IHostScreen _screen;
        private readonly List<ILight> _lights = new List<ILight>();
        private readonly List<BufferItem> _addedBuf = new List<BufferItem>();
        private readonly List<BufferItem> _removedBuf = new List<BufferItem>();

        public IHostScreen Screen => _screen;
        public int LightCount => _lights.Count;

        internal LightManager(IHostScreen screen)
        {
            _screen = screen;
        }

        internal void AddLight(ILight light, Action<ILight> callback)
        {
            Debug.Assert(Engine.IsThreadMain);
            Debug.Assert(light is not null);
            _addedBuf.Add(new(light, callback));
        }

        internal void RemoveLight(ILight light, Action<ILight> callback)
        {
            Debug.Assert(Engine.IsThreadMain);
            Debug.Assert(light is not null);
            _removedBuf.Add(new(light, callback));
        }

        internal void ApplyAdd()
        {
            Debug.Assert(Engine.IsThreadMain);
            if(_addedBuf.Count > 0) {
                ApplyAddPrivate();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ApplyRemove()
        {
            Debug.Assert(Engine.IsThreadMain);
            if(_removedBuf.Count > 0) {
                ApplyRemovePrivate();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]  // uncommon path, no inlining
        private void ApplyAddPrivate()
        {
            var lights = _lights;
            var addedBuf = _addedBuf;
            foreach(var (light, callback) in addedBuf.AsSpan()) {
                lights.Add(light);
                Debug.Assert(light.LifeState == LifeState.Activating);
                try {
                    callback.Invoke(light);
                }
                catch {
                    if(EngineSetting.UserCodeExceptionCatchMode == UserCodeExceptionCatchMode.Throw) { throw; }
                    // Don't throw. (Ignore exceptions in user code)
                }
            }
            addedBuf.Clear();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]  // uncommon path, no inlining
        private void ApplyRemovePrivate()
        {
            var lights = _lights;
            var removedBuf = _removedBuf;
            foreach(var (light, callback) in removedBuf.AsSpan()) {
                Debug.Assert(light.LifeState == LifeState.Terminating);
                // The light is not in the list if it failed to activate.
                lights.Remove(light);
                try {
                    callback.Invoke(light);
                }
                catch {
                    if(EngineSetting.UserCodeExceptionCatchMode == UserCodeExceptionCatchMode.Throw) { throw; }
                    // Don't throw. (Ignore exceptions in user code)
                }
            }
            removedBuf.Clear();
        }

        public ReadOnlySpan<ILight> GetLights() => _lights.AsReadOnlySpan();

        internal void Release()
        {
            _lights.Clear();
        }
    }
}
