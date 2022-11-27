#nullable enable
using Cysharp.Threading.Tasks;

namespace Elffy.Markup;

public interface IMarkupComponent<TSelf, TParent> where TSelf : class, IMarkupComponent<TSelf, TParent>
{
    static abstract UniTask<TSelf> Create(TParent parent);
}
