#nullable enable

namespace Elffy.Components
{
    // TODO: internal にする
    public interface IComponentInternal<T> : IComponent where T : class, IComponent
    {
        // TODO: internal にする
        public T Self { get; }
    }
}
