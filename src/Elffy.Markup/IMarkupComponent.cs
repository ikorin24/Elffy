#nullable enable
using Cysharp.Threading.Tasks;

namespace Elffy.Markup;

public interface IMarkupComponent<TSelf, TParent> where TSelf : IMarkupComponent<TSelf, TParent>
{
    static abstract UniTask<TSelf> Create(TParent parent);
}
