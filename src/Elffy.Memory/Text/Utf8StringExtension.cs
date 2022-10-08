#nullable enable
using System;
using Elffy.Effective;
using Elffy.Effective.Unsafes;

namespace Elffy.Text
{
    public static class Utf8StringExtension
    {
        /// <summary>Trim invisible charactors. (whitespace, '\t', '\r', and '\n')</summary>
        /// <returns>trimmed string</returns>
        public static ReadOnlySpan<byte> Trim(this ReadOnlySpan<byte> span)
        {
            return span.TrimStart().TrimEnd();
        }

        /// <summary>Trim invisible charactors of start. (whitespace, '\t', '\r', and '\n')</summary>
        /// <returns>trimmed string</returns>
        public static ReadOnlySpan<byte> TrimStart(this ReadOnlySpan<byte> span)
        {
            for(int i = 0; i < span.Length; i++) {
                ref readonly var p = ref span.At(i);
                if(p != ' ' && p != '\t' && p != '\r' && p != '\n') {
                    return span.SliceUnsafe(i, span.Length - i);
                }
            }
            return ReadOnlySpan<byte>.Empty;
        }

        /// <summary>Trim invisible charactors of end. (whitespace, '\t', '\r' and '\n')</summary>
        /// <returns>trimmed string</returns>
        public static ReadOnlySpan<byte> TrimEnd(this ReadOnlySpan<byte> span)
        {
            for(int i = span.Length - 1; i >= 0; i--) {
                ref readonly var p = ref span.At(i);
                if(p != ' ' && p != '\t' && p != '\r' && p != '\n') {
                    return span.SliceUnsafe(0, i + 1);
                }
            }
            return span;
        }

        /// <summary>Enumerate lines in the utf8 string. LF (\n) and CRLF (\r\n) are supported.</summary>
        /// <param name="str">utf8 string</param>
        /// <returns>strings of lines</returns>
        public static Utf8LineEnumerable Lines(this ReadOnlySpan<byte> str) => new Utf8LineEnumerable(str);

        /// <summary>Split the utf8 string by the specified character.</summary>
        /// <param name="str">utf8 string to split</param>
        /// <param name="separator">the separator charactor</param>
        /// <param name="options">split options</param>
        /// <returns>split strings</returns>
        public static SplitUtf8Strings Split(this ReadOnlySpan<byte> str, byte separator, StringSplitOptions options = StringSplitOptions.None)
        {
            return new SplitUtf8Strings(str, separator, options);
        }

        /// <summary>Split the utf8 string by the specified character.</summary>
        /// <param name="str">utf8 string to split</param>
        /// <param name="separator">the separator charactor</param>
        /// <param name="options">split options</param>
        public static SplitUtf8Strings Split(this ReadOnlySpan<byte> str, ReadOnlySpan<byte> separator, StringSplitOptions options = StringSplitOptions.None)
        {
            return new SplitUtf8Strings(str, separator, options);
        }

        /// <summary>Split the string by the speccified character</summary>
        /// <param name="str"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public static ReadOnlySpanTuple<byte, byte> Split2(this ReadOnlySpan<byte> str, byte separator)
        {
            for(int i = 0; i < str.Length; i++) {
                if(str[i] == separator) {
                    var latterStart = Math.Min(i + 1, str.Length);
                    return new(str.SliceUnsafe(0, i), str.SliceUnsafe(latterStart, str.Length - latterStart));
                }
            }
            return new(str, ReadOnlySpan<byte>.Empty);
        }

        public static ReadOnlySpanTuple<byte, byte> Split2(this ReadOnlySpan<byte> str, ReadOnlySpan<byte> separator)
        {
            if((uint)separator.Length > (uint)str.Length) {
                return new(str, ReadOnlySpan<byte>.Empty);
            }
            var maxLoop = str.Length - separator.Length + 1;
            for(int i = 0; i < maxLoop; i++) {
                if(str.SliceUnsafe(i, separator.Length).SequenceEqual(separator)) {
                    var latterStart = Math.Min(i + separator.Length, str.Length);
                    return new(str.SliceUnsafe(0, i), str.SliceUnsafe(latterStart, str.Length - latterStart));
                }
            }
            return new(str, ReadOnlySpan<byte>.Empty);
        }
    }
}
