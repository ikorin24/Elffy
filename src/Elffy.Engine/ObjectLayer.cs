#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Cysharp.Threading.Tasks;
using Elffy.Effective;
using Elffy.Threading;

namespace Elffy
{
    public abstract class ObjectLayer : PipelineOperation
    {
        private readonly List<FrameObject> _list;
        private readonly List<FrameObject> _addedBuf;
        private readonly List<FrameObject> _removedBuf;
        private readonly List<Positionable> _positionables;
        private EventSource<ObjectLayer> _objectAdded;
        private EventSource<ObjectLayer> _objectRemoved;

        public int ObjectCount => _list.Count;
        protected ReadOnlySpan<FrameObject> AddedObjects => _addedBuf.AsReadOnlySpan();
        protected ReadOnlySpan<FrameObject> RemovedObjects => _removedBuf.AsReadOnlySpan();
        protected ReadOnlySpan<Positionable> Positionables => _positionables.AsReadOnlySpan();

        public Event<ObjectLayer> ObjectAdded => _objectAdded.Event;
        public Event<ObjectLayer> ObjectRemoved => _objectRemoved.Event;

        protected ObjectLayer(int sortNumber, string? name) : base(sortNumber, name)
        {
            const int InitialCapacity = 32;

            _list = new List<FrameObject>(InitialCapacity);
            _addedBuf = new List<FrameObject>(InitialCapacity / 2);
            _removedBuf = new List<FrameObject>(InitialCapacity / 2);
            _positionables = new List<Positionable>(InitialCapacity);
        }

        public ReadOnlySpan<FrameObject> GetFrameObjects() => _list.AsReadOnlySpan();

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
            var identity = Matrix4.Identity;
            foreach(var positionable in _positionables.AsSpan()) {
                // Render only root objects.
                // Childen are rendered from thier parent method recursively.
                if(positionable.IsRoot == false) { continue; }
                try {
                    positionable.RenderShadowMapRecursively(identity, shadowMap);
                }
                catch {
                    if(EngineSetting.UserCodeExceptionCatchMode == UserCodeExceptionCatchMode.Throw) { throw; }
                    // Don't throw. (Ignore exceptions in user code)
                }
            }
        }

        protected sealed override void OnExecute(IHostScreen screen)
        {
            SelectMatrix(screen, out var view, out var projection);

            var identity = Matrix4.Identity;
            foreach(var positionable in _positionables.AsSpan()) {
                // Render only root objects.
                // Childen are rendered from thier parent method recursively.
                if(positionable.IsRoot == false) { continue; }
                try {
                    positionable.RenderRecursively(identity, view, projection);
                }
                catch {
                    if(EngineSetting.UserCodeExceptionCatchMode == UserCodeExceptionCatchMode.Throw) { throw; }
                    // Don't throw. (Ignore exceptions in user code)
                }
            }
        }

        internal void ApplyAdd()
        {
            if(_addedBuf.Count <= 0) { return; }

            _list.AddRange(_addedBuf);
            foreach(var item in _addedBuf.AsSpan()) {
                Debug.Assert(item.LifeState == LifeState.Activating);
                if(item.IsPositionable(out var positionable)) {
                    _positionables.Add(positionable);
                }
                try {
                    item.AddToObjectStoreCallback();
                }
                catch {
                    if(EngineSetting.UserCodeExceptionCatchMode == UserCodeExceptionCatchMode.Throw) { throw; }
                    // Don't throw. (Ignore exceptions in user code)
                }
            }
            _addedBuf.Clear();
            _objectAdded.InvokeIgnoreException(this);
        }

        internal void ApplyRemove()
        {
            if(_removedBuf.Count <= 0) { return; }

            var list = _list;
            var positionables = _positionables;

            foreach(var item in _removedBuf.AsSpan()) {
                Debug.Assert(item.LifeState == LifeState.Terminating);

                // The FrameObject is not in the list if it failed to activate.
                list.Remove(item);
                if(item.IsPositionable(out var positionable)) {
                    positionables.Remove(positionable);
                }
                try {
                    item.RemovedFromObjectStoreCallback();
                }
                catch {
                    if(EngineSetting.UserCodeExceptionCatchMode == UserCodeExceptionCatchMode.Throw) { throw; }
                    // Don't throw. (Ignore exceptions in user code)
                }
            }
            _removedBuf.Clear();
            _objectRemoved.InvokeIgnoreException(this);
        }

        internal void EarlyUpdate()
        {
            foreach(var frameObject in _list.AsSpan()) {
                if(frameObject.IsFrozen) { continue; }
                try {
                    frameObject.InvokeEarlyUpdate();
                }
                catch {
                    if(EngineSetting.UserCodeExceptionCatchMode == UserCodeExceptionCatchMode.Throw) { throw; }
                    // Don't throw. (Ignore exceptions in user code)
                }
            }
        }

        internal void Update()
        {
            foreach(var frameObject in _list.AsSpan()) {
                if(frameObject.IsFrozen) { continue; }
                try {
                    frameObject.InvokeUpdate();
                }
                catch {
                    if(EngineSetting.UserCodeExceptionCatchMode == UserCodeExceptionCatchMode.Throw) { throw; }
                    // Don't throw. (Ignore exceptions in user code)
                }
            }
        }

        internal void LateUpdate()
        {
            foreach(var frameObject in _list.AsSpan()) {
                if(frameObject.IsFrozen) { continue; }
                try {
                    frameObject.InvokeLateUpdate();
                }
                catch {
                    if(EngineSetting.UserCodeExceptionCatchMode == UserCodeExceptionCatchMode.Throw) { throw; }
                    // Don't throw. (Ignore exceptions in user code)
                }
            }
        }

        internal void ClearFrameObject()
        {
            // Abort added objects at the first.
            _addedBuf.Clear();

            // Terminate all living object. (Terminated objects go to removed buffer.)
            foreach(var item in _list.AsSpan()) {
                try {
                    item.Terminate().Forget();  // TODO: await
                }
                catch {
                    if(EngineSetting.UserCodeExceptionCatchMode == UserCodeExceptionCatchMode.Throw) { throw; }
                    // Don't throw. (Ignore exceptions in user code)
                }
            }

            // Apply removing
            ApplyRemove();

            // Clear all other lists
            _list.Clear();
            _removedBuf.Clear();
            _positionables.Clear();
        }

        internal void AddFrameObject(FrameObject frameObject)
        {
            Debug.Assert(TryGetScreen(out var screen));
            Debug.Assert(screen.CurrentTiming.IsOutOfFrameLoop() == false);
            Debug.Assert(frameObject is not null);
            _addedBuf.Add(frameObject);
        }

        internal void RemoveFrameObject(FrameObject frameObject)
        {
            Debug.Assert(frameObject is not null);
            _removedBuf.Add(frameObject);
        }
    }
}
