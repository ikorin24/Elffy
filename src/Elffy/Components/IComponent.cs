#nullable enable
using Elffy.Core;

namespace Elffy.Components
{
    /// <summary>interface of component owned by <see cref="ComponentOwner"/></summary>
    public interface IComponent
    {
        /// <summary>This method is called when <see cref="IComponent"/> is attached to <see cref="ComponentOwner"/></summary>
        /// <param name="owner"><see cref="ComponentOwner"/> instance</param>
        void OnAttached(ComponentOwner owner);

        /// <summary>This method is called when <see cref="IComponent"/> is detached from <see cref="ComponentOwner"/></summary>
        /// <param name="owner"><see cref="ComponentOwner"/> instance</param>
        void OnDetached(ComponentOwner owner);
    }
}
