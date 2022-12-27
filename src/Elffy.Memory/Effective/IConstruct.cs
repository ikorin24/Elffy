#nullable enable

namespace Elffy
{
    public interface IConstruct<TSelf, T> where TSelf : IConstruct<TSelf, T>
    {
        abstract static TSelf New(in T arg);
    }
}
