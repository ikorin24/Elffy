#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Elffy.Effective.Internal
{
    internal static class UnsafeTool
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static T SetValue<T>(in T target, T value) => Unsafe.AsRef(target) = value;
    }
}
