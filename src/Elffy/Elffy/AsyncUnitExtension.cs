#nullable enable
using Cysharp.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace Elffy
{
    public static class AsyncUnitExtension
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UniTask<AsyncUnit> AsCompletedTask(this AsyncUnit source)
        {
            return default;
        }
    }
}
