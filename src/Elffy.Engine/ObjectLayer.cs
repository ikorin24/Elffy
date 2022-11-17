#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Cysharp.Threading.Tasks;
using Elffy.Features.Internal;
using Elffy.Threading;

namespace Elffy
{
    public abstract class ObjectLayer : PipelineOperation
    {
        private readonly FrameObjectStore _store;

        public int ObjectCount => _store.ObjectCount;
        protected ReadOnlySpan<FrameObject> AddedObjects => _store.Added;
        protected ReadOnlySpan<FrameObject> RemovedObjects => _store.Removed;
        protected ReadOnlySpan<Positionable> Positionables => _store.Positionables;

        protected ObjectLayer(int sortNumber, string? name) : base(sortNumber, name)
        {
            _store = new FrameObjectStore(32);
        }

        public ReadOnlySpan<FrameObject> GetFrameObjects() => _store.List;

        public T FindObject<T>(string? name) where T : FrameObject
        {
            if(TryFindObject<T>(name, out var obj) == false) {
                ThrowNotFound(name);
                [DoesNotReturn] static void ThrowNotFound(string? name) => throw new ArgumentException($"{nameof(FrameObject)} '{name}' is not found");
            }
            return obj;
        }

        public FrameObject FindObject(string? name)
        {
            if(TryFindObject(name, out var obj) == false) {
                ThrowNotFound(name);
                [DoesNotReturn] static void ThrowNotFound(string? name) => throw new ArgumentException($"{nameof(FrameObject)} '{name}' is not found");
            }
            return obj;
        }

        public bool TryFindObject<T>(string? name, [MaybeNullWhen(false)] out T obj) where T : FrameObject
        {
            foreach(var frameObject in GetFrameObjects()) {
                if(frameObject.Name == name && frameObject is T x) {
                    obj = x;
                    return true;
                }
            }
            obj = default;
            return false;
        }

        public bool TryFindObject(string? name, [MaybeNullWhen(false)] out FrameObject obj)
        {
            foreach(var frameObject in GetFrameObjects()) {
                if(frameObject.Name == name) {
                    obj = frameObject;
                    return true;
                }
            }
            obj = default;
            return false;
        }


        protected virtual void SelectMatrix(IHostScreen screen, out Matrix4 view, out Matrix4 projection)
        {
            var camera = screen.Camera;
            view = camera.View;
            projection = camera.Projection;
        }

        private protected sealed override UniTask BeforeTerminating()
        {
            using var tasks = new ParallelOperation();
            foreach(var frameObject in GetFrameObjects()) {
                // non-root Positionable is terminated by its parent.
                if(frameObject.IsPositionable(out var positionable)) {
                    if(positionable.IsRoot) {
                        tasks.Add(CreateTerminationTask(positionable));
                    }
                }
                else {
                    tasks.Add(CreateTerminationTask(frameObject));
                }
            }
            return tasks.WhenAll();

            static async UniTask CreateTerminationTask(FrameObject frameObject)
            {
                try {
                    await frameObject.Terminate();
                }
                catch {
                    if(EngineSetting.UserCodeExceptionCatchMode == UserCodeExceptionCatchMode.Throw) { throw; }
                    // Ignore exceptions.
                }
            }
        }

        internal void RenderShadowMap(IHostScreen screen, CascadedShadowMap shadowMap)
        {
            _store.RenderShadowMap(shadowMap);
        }

        protected sealed override void OnExecute(IHostScreen screen)
        {
            SelectMatrix(screen, out var view, out var projection);
            _store.Render(view, projection);
        }

        internal void ApplyAdd() => _store.ApplyAdd();
        internal void ApplyRemove() => _store.ApplyRemove();
        internal void EarlyUpdate() => _store.EarlyUpdate();
        internal void Update() => _store.Update();
        internal void LateUpdate() => _store.LateUpdate();
        internal void ClearFrameObject() => _store.ClearFrameObject();

        internal void AddFrameObject(FrameObject frameObject)
        {
            Debug.Assert(TryGetScreen(out var screen));
            Debug.Assert(screen.CurrentTiming.IsOutOfFrameLoop() == false);
            _store.AddFrameObject(frameObject);
        }

        internal void RemoveFrameObject(FrameObject frameObject)
        {
            _store.RemoveFrameObject(frameObject);
        }
    }
}
