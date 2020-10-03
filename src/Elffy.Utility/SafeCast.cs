#nullable enable
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Elffy
{
    internal static class SafeCast
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T As<T>(object value) where T : class
        {
            Debug.Assert(value is T);
            return Unsafe.As<T>(value);
        }
    }
}
