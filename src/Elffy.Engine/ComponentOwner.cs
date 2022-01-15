#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using Elffy.Components;
using Elffy.Effective;
using Elffy.Features.Internal;

namespace Elffy
{
    /// <summary>Base class that has components.</summary>
    public abstract class ComponentOwner : FrameObject
    {
        private ComponentDictionary _dic;   // mutable object, don't make it readonly
        private FastSpinLock _lock;         // mutable object, don't make it readonly
        private bool _isDead;

        /// <summary>Event which fires when a component get attached.</summary>
        public event Action<ComponentOwner, IComponent>? ComponentAttached;
        /// <summary>Event which fires when a component get detached.</summary>
        public event Action<ComponentOwner, IComponent>? ComponentDetached;

        /// <summary>Get component of specified type</summary>
        /// <param name="type">component type</param>
        /// <returns>component object</returns>
        public IComponent GetComponent(Type type)
        {
            try {
                _lock.Enter();
                if(_dic.TryGetValue(type, out var component) == false) {
                    ThrowDoesNotHaveComponent(type);
                }
                return component;
            }
            finally {
                _lock.Exit();
            }

            [DoesNotReturn] static void ThrowDoesNotHaveComponent(Type type) => throw new InvalidOperationException($"No component of type '{type.FullName}'");
        }

        /// <summary>Get component of specified type</summary>
        /// <typeparam name="T">component type</typeparam>
        /// <returns>component object</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetComponent<T>() where T : class, IComponent
        {
            return SafeCast.As<T>(GetComponent(typeof(T)));
        }

        /// <summary>Try to get a component of specified type</summary>
        /// <param name="type">component type</param>
        /// <param name="component">component object</param>
        /// <returns>Returns true if the component was successfully retrieved, otherwise false.</returns>
        public bool TryGetComponent(Type type, [MaybeNullWhen(false)] out IComponent component)
        {
            try {
                _lock.Enter();
                return _dic.TryGetValue(type, out component);
            }
            finally {
                _lock.Exit();
            }
        }

        /// <summary>Try to get a component of specified type</summary>
        /// <typeparam name="T">component type</typeparam>
        /// <param name="component">component object</param>
        /// <returns>Returns true if the component was successfully retrieved, otherwise false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetComponent<T>([MaybeNullWhen(false)] out T component) where T : class, IComponent
        {
            Unsafe.SkipInit(out component);
            ref var c = ref Unsafe.As<T, IComponent>(ref component);
            return TryGetComponent(typeof(T), out c);
        }

        /// <summary>Add component object</summary>
        /// <typeparam name="T">component type</typeparam>
        /// <param name="component">component object</param>
        public void AddComponent<T>(T component) where T : class, IComponent
        {
            try {
                _lock.Enter();
                if(_isDead) {
                    ThrowAlreadyDead();
                }
                if(component is ISingleOwnerComponent soc && soc.Owner is not null) {
                    ThrowComponentAlreadyAttached();
                }
                if(_dic.TryAdd(component.GetType(), component) == false) {
                    ThrowAlreadyExists(component);
                }
            }
            finally {
                _lock.Exit();
            }

            component.OnAttached(this);
            ComponentAttached?.Invoke(this, component);

            [DoesNotReturn] static void ThrowAlreadyExists(T component) => throw new ArgumentException($"Component type '{component.GetType().FullName}' already exists.");
        }

        /// <summary>Add or replace component whose type is <typeparamref name="T"/>. Return true if replaced, otherwize false.</summary>
        /// <typeparam name="T">component type</typeparam>
        /// <param name="component">the component</param>
        /// <param name="old">old component</param>
        /// <returns>Return true if replaced, otherwize false.</returns>
        public bool AddOrReplaceComponent<T>(T component, [MaybeNullWhen(false)] out T old) where T : class, IComponent
        {
            Unsafe.SkipInit(out old);
            ref var o = ref Unsafe.As<T, IComponent>(ref old);
            bool replaced;
            try {
                _lock.Enter();
                if(_isDead) {
                    ThrowAlreadyDead();
                }
                if(component is ISingleOwnerComponent soc && soc.Owner is not null) {
                    ThrowComponentAlreadyAttached();
                }
                replaced = _dic.AddOrReplace(component.GetType(), component, out o);
            }
            finally {
                _lock.Exit();
            }

            if(replaced) {
                Debug.Assert(o is not null);
                ComponentDetached?.Invoke(this, o);
                o.OnDetached(this);
            }
            component.OnAttached(this);
            ComponentAttached?.Invoke(this, component);
            return replaced;
        }

        /// <summary>Get whether <see cref="ComponentOwner"/> has a component of specified type</summary>
        /// <param name="type">component type</param>
        /// <returns>True if <see cref="ComponentOwner"/> has the component; otherwise, false</returns>
        public bool HasComponent(Type type)
        {
            try {
                _lock.Enter();
                return _dic.ContainsKey(type);
            }
            finally {
                _lock.Exit();
            }
        }

        /// <summary>Get whether <see cref="ComponentOwner"/> has a component of specified type</summary>
        /// <typeparam name="T">component type</typeparam>
        /// <returns>True if <see cref="ComponentOwner"/> has the component; otherwise, false</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasComponent<T>() where T : class, IComponent
        {
            return HasComponent(typeof(T));
        }

        /// <summary>Remove the component of specified type.</summary>
        /// <param name="type">component type</param>
        /// <returns>True if the component is removed. False if the component does not exist.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool RemoveComponent(Type type) => RemoveComponent(type, out _);

        /// <summary>Remove the component of specified type.</summary>
        /// <param name="type">component type</param>
        /// <param name="component">removed component</param>
        /// <returns>True if the component is removed. False if the component does not exist.</returns>
        public bool RemoveComponent(Type type, [MaybeNullWhen(false)] out IComponent component)
        {
            bool isRemoved;
            try {
                _lock.Enter();
                if(_isDead) {
                    // If already dead, there are no components
                    Debug.Assert(_dic.Count == 0);
                    component = null;
                    return false;
                }
                isRemoved = _dic.Remove(type, out component);
            }
            finally {
                _lock.Exit();
            }

            if(isRemoved) {
                Debug.Assert(component is not null);
                ComponentDetached?.Invoke(this, component);
                component.OnDetached(this);
                return true;
            }
            else {
                return false;
            }
        }

        /// <summary>Remove the component of specified type.</summary>
        /// <typeparam name="T">component type</typeparam>
        /// <returns>True if the component is removed. False if the component does not exist.</returns>
        public bool RemoveComponent<T>() where T : class, IComponent => RemoveComponent<T>(out _);

        /// <summary>Remove the component of specified type.</summary>
        /// <typeparam name="T">component type</typeparam>
        /// <param name="component">removed component</param>
        /// <returns>True if the component is removed. False if the component does not exist.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool RemoveComponent<T>([MaybeNullWhen(false)] out T component) where T : class, IComponent
        {
            Unsafe.SkipInit(out component);
            ref var c = ref Unsafe.As<T, IComponent>(ref component);
            return RemoveComponent(typeof(T), out c);
        }

        internal IComponent[] GetAllComponents()
        {
            try {
                _lock.Enter();
                var values = _dic.Values;
                var components = new IComponent[values.Count];
                values.CopyTo(components);
                return components;
            }
            finally {
                _lock.Exit();
            }
        }

        protected override unsafe void OnDead()
        {
            base.OnDead();

            _lock.Enter();
            _dic.Clear(&OnCleared, &EachItemAfterCleared, this);

            static void OnCleared(ComponentOwner owner)
            {
                owner._isDead = true;
                owner._lock.Exit();
            }

            static void EachItemAfterCleared(ComponentOwner owner, IComponent component)
            {
                try {
                    owner.ComponentDetached?.Invoke(owner, component);
                    component.OnDetached(owner);
                }
                catch {
                    if(EngineSetting.UserCodeExceptionCatchMode == UserCodeExceptionCatchMode.Throw) { throw; }
                    // Don't throw. Ignore exceptions in user code.
                }
            }
        }

        [DoesNotReturn]
        private static void ThrowComponentAlreadyAttached() => throw new ArgumentException($"The component is already attached to some {nameof(ComponentOwner)}.");

        [DoesNotReturn]
        private static void ThrowAlreadyDead() => throw new InvalidOperationException($"{nameof(FrameObject)} is already dead.");
    }
}
