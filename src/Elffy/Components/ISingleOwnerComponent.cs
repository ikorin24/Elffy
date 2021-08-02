#nullable enable
using System;
using Elffy.Core;

namespace Elffy.Components
{
    /// <summary>interface of component which is owned just by a single <see cref="ComponentOwner"/> instance</summary>
    public interface ISingleOwnerComponent : IComponent, IDisposable
    {
        /// <summary>Get instance which owns this object</summary>
        ComponentOwner? Owner { get; }

        /// <summary>Get whether this object is automatically disposed on <see cref="IComponent.OnDetached(ComponentOwner)"/></summary>
        bool AutoDisposeOnDetached { get; }
    }
}
