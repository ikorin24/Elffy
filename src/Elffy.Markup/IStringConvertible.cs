#nullable enable

namespace Elffy.Markup;

public interface IStringConvertible<TSelf>
{
    abstract static TSelf Convert(string value);
}
