#nullable enable
namespace Elffy.Core
{
    public interface IDestroyable
    {
        bool IsDestroyed { get; }
        void Destroy();
    }
}
