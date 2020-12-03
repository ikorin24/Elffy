#nullable enable
using Elffy.Effective.Unsafes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Elffy.Core
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

        private FrameObjectStore(int dummyArg)
        {
            _list = new List<FrameObject>();
            _addedBuf = new List<FrameObject>();
            _removedBuf = new List<FrameObject>();
            _renderables = new List<Renderable>();
        }

        /// <summary>Create new <see cref="FrameObjectStore"/></summary>
        /// <returns>instance</returns>
        public static FrameObjectStore New()
        {
            return new FrameObjectStore(0);
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
                frameObject.EarlyUpdate();
            }
        }

        /// <summary>Update frame</summary>
        public void Update()
        {
            foreach(var frameObject in _list.AsSpan()) {
                if(frameObject.IsFrozen) { continue; }
                frameObject.Update();
            }
        }

        /// <summary>Late update frame</summary>
        public void LateUpdate()
        {
            foreach(var frameObject in _list.AsSpan()) {
                if(frameObject.IsFrozen) { continue; }
                frameObject.LateUpdate();
            }
        }

        /// <summary>Clear all <see cref="FrameObject"/> in lists</summary>
        public void ClearFrameObject()
        {
            // Abort added objects at the first.
            _addedBuf.Clear();

            // Terminate all living object. (Terminated objects go to removed buffer.)
            foreach(var item in _list.AsSpan()) {
                item.Terminate();
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
            foreach(var item in _removedBuf.AsSpan()) {
                Debug.Assert(item.LifeState == FrameObjectLifeState.Terminated);
                var sucessRemoving = _list.Remove(item);
                Debug.Assert(sucessRemoving);
                switch(item) {
                    case Renderable renderable: {
                        var renderableRemoved = _renderables.Remove(renderable);
                        Debug.Assert(renderableRemoved);
                        break;
                    }
                }
                item.RemovedFromObjectStoreCallback();
            }
            _removedBuf.Clear();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]  // uncommon path, no inlining
        private void ApplyAddPrivate()
        {
            _list.AddRange(_addedBuf);
            foreach(var item in _addedBuf.AsSpan()) {
                Debug.Assert(item.LifeState == FrameObjectLifeState.Activated);
                switch(item) {
                    case Renderable renderable:
                        _renderables.Add(renderable);
                        break;
                }
                item.AddToObjectStoreCallback();
            }
            _addedBuf.Clear();
        }
    }
}
