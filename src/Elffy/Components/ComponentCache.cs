#nullable enable
using Elffy.Core;
using System;

namespace Elffy.Components
{
    /// <summary>Cache field of <see cref="IComponent"/></summary>
    /// <typeparam name="T">Type of component</typeparam>
    public struct ComponentCache<T> where T : class, IComponent
    {
        private T? _component;

        /// <summary>Get component of type <see cref="T"/>. (Return null if no component.)</summary>
        public readonly T? Value => _component;

        public ComponentCache(ComponentOwner componentOwner)
        {
            if(componentOwner is null) { throw new ArgumentNullException(nameof(componentOwner)); }

            // componentOwner を保持する必要はないので持たない (もし持つ必要が出てきた場合は弱参照にすること)
            _component = null;
            componentOwner.ComponentAttached += OwnerComponentAttached;
            componentOwner.ComponentDetached += OwnerComponentDetached;
        }

        private void OwnerComponentAttached(ComponentOwner sender, IComponent e)
        {
            if(e is T component) {
                _component = component;
            }
        }

        private void OwnerComponentDetached(ComponentOwner sender, IComponent e)
        {
            if(_component == e) {
                _component = null;
            }
        }
    }
}
