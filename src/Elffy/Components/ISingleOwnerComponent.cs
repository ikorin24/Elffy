#nullable enable
using Elffy.Core;

namespace Elffy.Components
{
    /// <summary>interface of component which is owned just by a single <see cref="ComponentOwner"/> instance</summary>
    public interface ISingleOwnerComponent : IComponent
    {
        /// <summary>Get instance which owns this object</summary>
        ComponentOwner? Owner { get; }

        /// <summary>Get whether this object is automatically disposed in the case that it inherits <see cref="System.IDisposable"/></summary>
        bool AutoDisposeOnDetached { get; }
    }
}
