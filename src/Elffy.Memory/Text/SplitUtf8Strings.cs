#nullable enable
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Elffy.Text
{
    [DebuggerTypeProxy(typeof(Utf8StringsDebuggerTypeProxy))]
    [DebuggerDisplay("ReadOnlySpan<byte>[{Count()}]")]
    public readonly ref struct SplitUtf8Strings
    {
        private readonly ReadOnlySpan<byte> _str;
        private readonly bool _isSeparatorSingleByte;
        private readonly byte _separator;
        private readonly ReadOnlySpan<byte> _spanSeparator;
        private readonly StringSplitOptions _options;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SplitUtf8Strings(ReadOnlySpan<byte> str, byte separator, StringSplitOptions options)
        {
            _str = str;
            _isSeparatorSingleByte = true;
            _separator = separator;
            _spanSeparator = default;
            _options = options;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SplitUtf8Strings(ReadOnlySpan<byte> str, ReadOnlySpan<byte> separator, StringSplitOptions options)
        {
            _str = str;
            _isSeparatorSingleByte = false;
            _separator = default;
            _spanSeparator = separator;
            _options = options;
        }

        /// <summary>Count the strings. (The operation is O(N), N is the length of the original string.)</summary>
        /// <returns>number of the strings</returns>
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator() => _isSeparatorSingleByte ? new Enumerator(_str, _separator, _options) : new Enumerator(_str, _spanSeparator, _options);

        public ref struct Enumerator
        {
            private ReadOnlySpan<byte> _str;
            private ReadOnlySpan<byte> _current;
            private readonly bool _isSeparatorChar;
            private readonly byte _separatorChar;
            private readonly ReadOnlySpan<byte> _separatorStr;
            private readonly StringSplitOptions _options;

            public ReadOnlySpan<byte> Current => _current;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Enumerator(ReadOnlySpan<byte> str, byte separator, StringSplitOptions options)
            {
                _str = str;
                _current = ReadOnlySpan<byte>.Empty;
                _isSeparatorChar = true;
                _separatorChar = separator;
                _separatorStr = default;
                _options = options;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Enumerator(ReadOnlySpan<byte> str, ReadOnlySpan<byte> separator, StringSplitOptions options)
            {
                _str = str;
                _current = ReadOnlySpan<byte>.Empty;
                _isSeparatorChar = false;
                _separatorChar = default;
                _separatorStr = separator;
                _options = options;
            }

            public void Dispose() { }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
#if NET5_0_OR_GREATER
            Next:
#endif
                if(_str.IsEmpty) {
                    return false;
                }
                if(_isSeparatorChar) {
                    (_current, _str) = _str.Split2(_separatorChar);
                }
                else {
                    (_current, _str) = _str.Split2(_separatorStr);
                }

                if((_options & StringSplitOptions.RemoveEmptyEntries) == StringSplitOptions.RemoveEmptyEntries) {
                    _current = _current.Trim();
                }
#if NET5_0_OR_GREATER
                if((_options & StringSplitOptions.TrimEntries) == StringSplitOptions.TrimEntries) {
                    goto Next;
                }
#endif
                return true;
            }
        }
    }

    internal sealed class Utf8StringsDebuggerTypeProxy
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly string[] _array;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public string[] Items => _array;


        public Utf8StringsDebuggerTypeProxy(Utf8StringLines lines)
        {
            _array = lines.ToStringArray();
        }

        public Utf8StringsDebuggerTypeProxy(SplitUtf8Strings strings)
        {
            _array = strings.ToStringArray();
        }
    }
}
