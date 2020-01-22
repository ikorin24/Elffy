#nullable enable
using Elffy.Effective;
using Elffy.Exceptions;
using Elffy.Components;
using System;
using System.Runtime.CompilerServices;

namespace Elffy.Core
{
    /// <summary>Base class that has components.</summary>
    public abstract class ComponentOwner : FrameObject
    {
        /// <summary>Get component of specified type</summary>
        /// <typeparam name="T">component type</typeparam>
        /// <returns>component object</returns>
        public T GetComponent<T>() where T : class, IComponent
        {
            if(!ComponentStore<T>.TryGet(this, out var component)) {
                throw new InvalidOperationException($"No component of type '{typeof(T).FullName}'");
            }
            return component;
        }

        /// <summary>Add component object</summary>
        /// <typeparam name="T">component type</typeparam>
        /// <param name="component">component object</param>
        public void AddComponent<T>(T component) where T : class, IComponent
        {
            ArgumentChecker.ThrowIfNullArg(component, nameof(component));
            ArgumentChecker.ThrowArgumentIf(ComponentStore<T>.HasComponentOf(this), $"Component type '{typeof(T).FullName.AsInterned()}' already exists.".AsInterned());
            ComponentStore<T>.Add(this, component);
            component.OnAttached(this);
        }

        /// <summary>Add or replace component whose type is <typeparamref name="T"/>. Return true if replaced, otherwize false.</summary>
        /// <typeparam name="T">component type</typeparam>
        /// <param name="component">the component</param>
        /// <returns>Return true if replaced, otherwize false.</returns>
        public bool AddOrReplaceComponent<T>(T component) where T : class, IComponent
        {
            ArgumentChecker.ThrowIfNullArg(component, nameof(component));
            var addNew = ComponentStore<T>.AddOrReplace(this, component);
            if(addNew) {
                component.OnAttached(this);
            }
            return addNew;
        }

        /// <summary>Get whether <see cref="ComponentOwner"/> has a component of specified type</summary>
        /// <typeparam name="T">component type</typeparam>
        /// <returns>True if <see cref="ComponentOwner"/> has the component; otherwise, false</returns>
        public bool HasComponent<T>() where T : class, IComponent
        {
            return ComponentStore<T>.HasComponentOf(this);
        }

        /// <summary>Remove the component of specified type.</summary>
        /// <typeparam name="T">component type</typeparam>
        /// <returns>True if the component is removed. False if the component does not exist.</returns>
        public bool RemoveComponent<T>() where T : class, IComponent
        {
            var removed = ComponentStore<T>.Remove(this, out var component);
            if(removed) {
                component.OnDetached(this);
            }
            return removed;
        }


        /// <summary>
        /// Component store of type <typeparamref name="T"/>.<para/>
        /// The reference to an instance of dictionary key is weak refarence.<para/>
        /// </summary>
        /// <typeparam name="T">component type</typeparam>
        private static class ComponentStore<T> where T : class, IComponent
        {
            /// <summary>
            /// Weak reference dictionary of (owner, component) pair.<para/>
            /// Reference to key is weak reference. The table is removed automatically when whose key is collected by GC.<para/>
            /// </summary>
            private static readonly ConditionalWeakTable<ComponentOwner, T> _components = new ConditionalWeakTable<ComponentOwner, T>();

            /// <summary>Get whether specified owner has component whose type is <typeparamref name="T"/>.</summary>
            /// <param name="owner">the owner of the component</param>
            /// <returns>Return true if the owner has component of <typeparamref name="T"/>, otherwize false.</returns>
            public static bool HasComponentOf(ComponentOwner owner)
            {
                return _components.TryGetValue(owner, out _);
            }

            /// <summary>Add component whose type is <typeparamref name="T"/></summary>
            /// <param name="owner">the owner of the component</param>
            /// <param name="component">the component</param>
            public static void Add(ComponentOwner owner, T component)
            {
                _components.Add(owner, component);
            }

            /// <summary>Add or replace component whose type is <typeparamref name="T"/>. Return true if replaced, otherwize false.</summary>
            /// <param name="owner">the owner of the component</param>
            /// <param name="component">the component</param>
            /// <returns>Return true if replaced, otherwize false.</returns>
            public static bool AddOrReplace(ComponentOwner owner, T component)
            {
                if(_components.TryGetValue(owner, out _)) {
                    _components.Remove(owner);
                    _components.Add(owner, component);
                    return true;
                }
                else {
                    _components.Add(owner, component);
                    return false;
                }
            }

            /// <summary>Get component of specified owner.</summary>
            /// <param name="owner">the owner of the component</param>
            /// <returns>the component</returns>
            public static bool TryGet(ComponentOwner owner, out T value)
            {
                return _components.TryGetValue(owner, out value);
            }

            /// <summary>Remove component of specified owner.</summary>
            /// <param name="owner">the owner of the component</param>
            /// <param name="value">removed component</param>
            /// <returns>Return true if removed. Return false when the owner has no component.</returns>
            public static bool Remove(ComponentOwner owner, out T value)
            {
                _components.TryGetValue(owner, out value);
                return _components.Remove(owner);
            }
        }
    }
}
