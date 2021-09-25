#nullable enable
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Elffy.Effective.Unsafes;

namespace Elffy.Text
{
    [DebuggerTypeProxy(typeof(Utf8StringsDebuggerTypeProxy))]
    [DebuggerDisplay("ReadOnlySpan<byte>[{Count()}]")]
    public readonly ref struct Utf8StringLines
    {
        private readonly ReadOnlySpan<byte> _str;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Utf8StringLines(ReadOnlySpan<byte> str) => _str = str;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator() => new Enumerator(_str);

        /// <summary>Count the lines. (The operation is O(N), N is the length of the original string.)</summary>
        /// <returns>number of the lines</returns>
        public int Count()
        {
            var count = 0;
            foreach(var line in this) {
                count++;
            }
            return count;
        }

        /// <summary>Copy to the array of <see cref="string"/></summary>
        /// <returns>array of <see cref="string"/></returns>
        public string[] ToStringArray()
        {
            var array = new string[Count()];
            var i = 0;
            foreach(var line in this) {
                array[i++] = Utf8StringHelper.ToString(line);
            }
            return array;
        }

        public ref struct Enumerator
        {
            private ReadOnlySpan<byte> _str;
            private ReadOnlySpan<byte> _current;

            public ReadOnlySpan<byte> Current => _current;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Enumerator(ReadOnlySpan<byte> str)
            {
                _str = str;
                _current = ReadOnlySpan<byte>.Empty;
            }

            public void Dispose() { }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                if(_str.IsEmpty) {
                    return false;
                }
                (_current, _str) = _str.Split2((byte)'\n');
                if(_current.IsEmpty == false && _current.At(_current.Length - 1) == '\r') {
                    _current = _current.SliceUnsafe(0, _current.Length - 1);
                }
                return true;
            }
        }
    }
}
