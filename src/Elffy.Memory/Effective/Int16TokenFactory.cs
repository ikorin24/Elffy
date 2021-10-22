#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Elffy.Effective
{
    public struct Int16TokenFactory : IEquatable<Int16TokenFactory>
    {
        private int _tokenFactory;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short CreateToken()
        {
            return (short)Interlocked.Increment(ref _tokenFactory);
        }

        public override bool Equals(object? obj) => obj is Int16TokenFactory factory && Equals(factory);

        public bool Equals(Int16TokenFactory other) => _tokenFactory == other._tokenFactory;

        public override int GetHashCode() => _tokenFactory.GetHashCode();
    }
}
