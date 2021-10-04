#nullable enable
using System.Threading;

namespace Elffy.Threading
{
    internal static class CancellationTokenHelper
    {
        public static CancellationToken Combine(CancellationToken token1, CancellationToken token2)
        {
            if(!token1.CanBeCanceled) {
                // token1 == default
                return token2;
            }
            if(!token2.CanBeCanceled) {
                // token2 == default
                return token1;
            }
            return CancellationTokenSource.CreateLinkedTokenSource(token1, token2).Token;
        }
    }
}
