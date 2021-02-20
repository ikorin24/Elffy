#nullable enable
using System;
using System.ComponentModel;

namespace Elffy.Effective.Unsafes
{
    // [NOTE]
    // No one can make an instance of type 'NullLiteral' in the usual way.
    // The class is just used as null literal.
    //
    // Example:
    // 
    // 'SomeStruct' can be casted from 'NullLiteral'.
    // And it can be compared to 'NullLiteral'.
    //
    //     public struct SomeStruct
    //     {
    //         public static implicit operator SomeStruct(NullLiteral? @null) => default;
    //         public static operator ==(SomeStruct some, NullLiteral? @null) => ...
    //         public static operator !=(SomeStruct some, NullLiteral? @null) => ...
    //     }
    //
    // And set null literal
    // 
    //     void SomeMethod()
    //     {
    //         SomeStruct foo = null;
    //         
    //         if(foo == null) {
    //             
    //         }
    //     }
    //

    /// <summary>The class that represents <see langword="null"/> literal.</summary>
    /// <remarks>No one can make an instance. Don't use this class explicitly.</remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]   // Users should not see the class, but it must be public class.
    public sealed class NullLiteral
    {
        private NullLiteral() { throw new InvalidOperationException("Hey! How did you get here?"); }
    }
}
