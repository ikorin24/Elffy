using Elffy.Exceptions;
using System;
using System.Collections.Generic;

namespace Elffy.Core
{
    /// <summary>
    /// Component を持つことができるクラスの基底
    /// </summary>
    public abstract class ComponentOwner : FrameObject
    {
        private List<object> _components = new List<object>();
        private List<Type> _types = new List<Type>();

        #region GetComponent
        /// <summary>Get component of specified type</summary>
        /// <typeparam name="T">component type</typeparam>
        /// <returns>component object</returns>
        public T GetComponent<T>()
        {
            return (T)_components.Find(x => x is T);
        }
        #endregion

        #region AddComponent
        /// <summary>Add component object</summary>
        /// <typeparam name="T">component type</typeparam>
        /// <param name="component">component object</param>
        public void AddComponent<T>(T component)
        {
            ExceptionManager.ThrowIfNullArg(component, nameof(component));
            var type = typeof(T);
            ExceptionManager.ThrowIf(_types.Contains(type), new ArgumentException($"Component type '{type.FullName}' already exists."));

            // _types と _component は同じ index は同じ要素に対応している前提
            // 同じ型の Component は複数存在しない前提

            _types.Add(type);
            _components.Add(component);
        }
        #endregion

        #region HasComponent
        /// <summary>Get whether <see cref="ComponentOwner"/> has a component of specified type</summary>
        /// <typeparam name="T">component type</typeparam>
        /// <returns>True if <see cref="ComponentOwner"/> has the component; otherwise, false</returns>
        public bool HasComponent<T>()
        {
            return _types.Contains(typeof(T));
        }
        #endregion

        #region RemoveComponent
        /// <summary>Remove the component of specified type.</summary>
        /// <typeparam name="T">component type</typeparam>
        /// <returns>True if the component is removed. False if the component does not exist.</returns>
        public bool RemoveComponent<T>()
        {
            // _types と _component は同じ index は同じ要素に対応している前提
            // 同じ型の Component は複数存在しない前提
            for(int i = 0; i < _types.Count; i++) {
                if(_types[i] == typeof(T)) {
                    _types.RemoveAt(i);
                    _components.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }
        #endregion
    }
}
