#nullable enable
using Elffy.AssemblyServices;
using System.Runtime.CompilerServices;

namespace Elffy.Effective.Unsafes
{
    internal static class StringUnsafeExtension
    {
        /// <summary>Get reference to first char of <see cref="string"/></summary>
        /// <param name="source">source <see cref="string"/></param>
        /// <returns>reference to first char</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CriticalDotnetDependency("netcoreapp3.1")]
        public static ref char GetFirstCharReference(this string source)
        {
            return ref Unsafe.As<StringDummy>(source)._firstChar;
        }

        private sealed class StringDummy
        {
#pragma warning disable CS0649  // warning of field not set
            internal readonly int _stringLength;
            internal char _firstChar;
#pragma warning restore CS0649

            private StringDummy()
            {

            }
        }
    }
}
