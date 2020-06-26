#nullable enable
using Elffy.Core;

namespace Elffy.Components
{
    public interface ISingleOwnerComponent : IComponent
    {
        ComponentOwner? Owner { get; }
    }
}
