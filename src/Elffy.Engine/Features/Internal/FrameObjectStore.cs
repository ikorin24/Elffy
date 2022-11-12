#nullable enable
using Cysharp.Threading.Tasks;
using Elffy.Effective;
using Elffy.AssemblyServices;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Elffy.Features.Internal
{
    [DontUseDefault]
    internal readonly struct FrameObjectStore
    {
        private readonly List<FrameObject> _list;
        private readonly List<FrameObject> _addedBuf;
        private readonly List<FrameObject> _removedBuf;
        private readonly List<Positionable> _positionables;

        public int ObjectCount => _list.Count;
        public ReadOnlySpan<FrameObject> List => _list.AsReadOnlySpan();
        public ReadOnlySpan<FrameObject> Added => _addedBuf.AsReadOnlySpan();
        public ReadOnlySpan<FrameObject> Removed => _removedBuf.AsReadOnlySpan();
        public ReadOnlySpan<Positionable> Positionables => _positionables.AsReadOnlySpan();

        public FrameObjectStore(int capacityHint)
        {
            Debug.Assert(capacityHint >= 0);
            _list = new List<FrameObject>(capacityHint);
            _addedBuf = new List<FrameObject>(capacityHint / 2);
            _removedBuf = new List<FrameObject>(capacityHint / 2);
            _positionables = new List<Positionable>(capacityHint);
        }

        [Obsolete("Don't use default constructor", true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public FrameObjectStore() => throw new NotSupportedException("Don't use default constructor");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddFrameObject(FrameObject frameObject)
        {
            Debug.Assert(frameObject is null == false);
            _addedBuf.Add(frameObject);
        }

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
                    frameObject.InvokeEarlyUpdate();
                }
                catch {
                    if(EngineSetting.UserCodeExceptionCatchMode == UserCodeExceptionCatchMode.Throw) { throw; }
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
                    frameObject.InvokeUpdate();
                }
                catch {
                    if(EngineSetting.UserCodeExceptionCatchMode == UserCodeExceptionCatchMode.Throw) { throw; }
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
                    frameObject.InvokeLateUpdate();
                }
                catch {
                    if(EngineSetting.UserCodeExceptionCatchMode == UserCodeExceptionCatchMode.Throw) { throw; }
                    // Don't throw. (Ignore exceptions in user code)
                }
            }
        }

        public void Render(in Matrix4 view, in Matrix4 projection)
        {
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

        public void RenderShadowMap(CascadedShadowMap shadowMap)
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

        /// <summary>Clear all <see cref="FrameObject"/> in lists</summary>
        public void ClearFrameObject()
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

        [MethodImpl(MethodImplOptions.NoInlining)]  // uncommon path, no inlining
        private void ApplyRemovePrivate()
        {
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
        }

        [MethodImpl(MethodImplOptions.NoInlining)]  // uncommon path, no inlining
        private void ApplyAddPrivate()
        {
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
        }
    }
}
