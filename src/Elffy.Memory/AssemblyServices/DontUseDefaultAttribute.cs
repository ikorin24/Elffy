#nullable enable
using System;
using System.Diagnostics;

namespace Elffy.AssemblyServices
{
    [Conditional("COMPILE_TIME_ONLY")]  // This attribute is not included in the assembly.
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
    internal sealed class DontUseDefaultAttribute : Attribute
    {
        public DontUseDefaultAttribute()
        {
        }
    }
}
