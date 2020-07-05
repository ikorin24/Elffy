#nullable enable

namespace Elffy.Components
{
    internal interface IComponentInternal<T> : IComponent where T : class, IComponent
    {
        internal T Self { get; }
    }
}
