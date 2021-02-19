#nullable enable
using System.ComponentModel;

namespace Elffy.Effective.Unsafes
{
    // [NOTE]
    // No one can make a instance of type 'NullLiteral' in the usual way.
    // The class is just used as null literal.
    //
    // Exsample:
    // 
    // 'SomeType' can be casted from 'NullLiteral'. (SomeType is struct)
    //
    //     public struct SomeType
    //     {
    //         public static implicit operator SomeType(NullLiteral? @null) => default;
    //     }
    //
    // And set null literal
    // 
    //     void SomeMethod()
    //     {
    //         SomeType foo = null;
    //     }
    //

    [EditorBrowsable(EditorBrowsableState.Never)]   // Users should not see the class, but it must be public class.
    public sealed class NullLiteral
    {
        private NullLiteral() { }
    }
}
