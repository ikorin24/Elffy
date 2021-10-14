#nullable enable
using Elffy.Effective;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Elffy.Features.Internal
{
    internal readonly struct FrameObjectStore
    {
        private readonly List<FrameObject> _list;
        private readonly List<FrameObject> _addedBuf;
        private readonly List<FrameObject> _removedBuf;
        private readonly List<Renderable> _renderables;

        public int ObjectCount => _list.Count;
        public ReadOnlySpan<FrameObject> List => _list.AsReadOnlySpan();
        public ReadOnlySpan<FrameObject> Added => _addedBuf.AsReadOnlySpan();
        public ReadOnlySpan<FrameObject> Removed => _removedBuf.AsReadOnlySpan();
        public ReadOnlySpan<Renderable> Renderables => _renderables.AsReadOnlySpan();

        private FrameObjectStore(int capacityHint)
        {
            Debug.Assert(capacityHint >= 0);
            _list = new List<FrameObject>(capacityHint);
            _addedBuf = new List<FrameObject>(capacityHint / 2);
            _removedBuf = new List<FrameObject>(capacityHint / 2);
            _renderables = new List<Renderable>(capacityHint);
        }

        /// <summary>Create new <see cref="FrameObjectStore"/></summary>
        /// <param name="capacityHint">capacity hint</param>
        /// <returns>instance</returns>
        public static FrameObjectStore New(int capacityHint)
        {
            return new FrameObjectStore(capacityHint);
        }

        /// <summary>指定した<see cref="FrameObject"/>を追加します</summary>
        /// <param name="frameObject">追加するオブジェクト</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddFrameObject(FrameObject frameObject)
        {
            Debug.Assert(frameObject is null == false);
            _addedBuf.Add(frameObject);
        }

        /// <summary>指定した<see cref="FrameObject"/>を削除します</summary>
        /// <param name="frameObject">削除するオブジェクト</param>
        /// <returns>削除できたかどうか</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveFrameObject(FrameObject frameObject)
        {
            Debug.Assert(frameObject is null == false);
            _removedBuf.Add(frameObject);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ApplyAdd()
        {
            if(_addedBuf.Count > 0) {
                ApplyAddPrivate();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ApplyRemove()
        {
            if(_removedBuf.Count > 0) {
                ApplyRemovePrivate();
            }
        }

        /// <summary>Early update frame</summary>
        public void EarlyUpdate()
        {
            foreach(var frameObject in _list.AsSpan()) {
                if(frameObject.IsFrozen) { continue; }
                try {
                    frameObject.EarlyUpdate();
                }
                catch {
                    // Don't throw. (Ignore exceptions in user code)
                }
            }
        }

        /// <summary>Update frame</summary>
        public void Update()
        {
            foreach(var frameObject in _list.AsSpan()) {
                if(frameObject.IsFrozen) { continue; }
                try {
                    frameObject.Update();
                }
                catch {
                    // Don't throw. (Ignore exceptions in user code)
                }
            }
        }

        /// <summary>Late update frame</summary>
        public void LateUpdate()
        {
            foreach(var frameObject in _list.AsSpan()) {
                if(frameObject.IsFrozen) { continue; }
                try {
                    frameObject.LateUpdate();
                }
                catch {
                    // Don't throw. (Ignore exceptions in user code)
                }
            }
        }

        public void Render(in Matrix4 view, in Matrix4 projection)
        {
            var identity = Matrix4.Identity;
            foreach(var renderable in _renderables.AsSpan()) {
                // Render only root objects.
                // Childen are rendered from thier parent 'Render' method.
                if(renderable.IsRoot == false) { continue; }
                try {
                    renderable.Render(identity, view, projection);
                }
                catch {
                    // Don't throw. (Ignore exceptions in user code)
                }
            }
        }

        /// <summary>Clear all <see cref="FrameObject"/> in lists</summary>
        public void ClearFrameObject()
        {
            // Abort added objects at the first.
            _addedBuf.Clear();

            // Terminate all living object. (Terminated objects go to removed buffer.)
            foreach(var item in _list.AsSpan()) {
                try {
                    item.Terminate();
                }
                catch {
                    // Don't throw. (Ignore exceptions in user code)
                }
            }

            // Apply removing
            ApplyRemove();

            // Clear all other lists
            _list.Clear();
            _removedBuf.Clear();
            _renderables.Clear();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]  // uncommon path, no inlining
        private void ApplyRemovePrivate()
        {
            var list = _list;
            var renderables = _renderables;

            foreach(var item in _removedBuf.AsSpan()) {
                Debug.Assert(item.LifeState == LifeState.Terminating);

                // The FrameObject is not in the list if it failed to activate.
                list.Remove(item);
                if(item is Renderable renderable) {
                    renderables.Remove(renderable);
                }
                try {
                    item.RemovedFromObjectStoreCallback();
                }
                catch {
                    // Don't throw. (Ignore exceptions in user code)
                }
            }
            _removedBuf.Clear();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]  // uncommon path, no inlining
        private void ApplyAddPrivate()
        {
            _list.AddRange(_addedBuf);
            foreach(var item in _addedBuf.AsSpan()) {
                Debug.Assert(item.LifeState == LifeState.Activating);
                if(item is Renderable renderable) {
                    _renderables.Add(renderable);
                }
                try {
                    item.AddToObjectStoreCallback();
                }
                catch {
                    // Don't throw. (Ignore exceptions in user code)
                }
            }
            _addedBuf.Clear();
        }
    }
}
