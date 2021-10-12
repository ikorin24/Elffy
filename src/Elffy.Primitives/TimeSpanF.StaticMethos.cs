#nullable enable
#if NET5_0_OR_GREATER
#define CAN_SKIP_INIT
#endif
using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Elffy
{
    partial struct TimeSpanF
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator TimeSpanF(TimeSpan timeSpan) => new TimeSpanF(timeSpan);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator TimeSpan(TimeSpanF timeSpan) => new TimeSpan(timeSpan.Ticks);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe ref TimeSpan OutAsTimeSpanF(out TimeSpanF timeSpan)
        {
#if CAN_SKIP_INIT
            Unsafe.SkipInit(out timeSpan);
#else
            timeSpan = default;
#endif
            return ref Unsafe.As<TimeSpanF, TimeSpan>(ref timeSpan);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TimeSpanF FromDays(float value) => TimeSpan.FromDays(value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TimeSpanF FromDays(double value) => TimeSpan.FromDays(value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TimeSpanF FromHours(float value) => TimeSpan.FromHours(value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TimeSpanF FromHours(double value) => TimeSpan.FromHours(value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TimeSpanF FromMilliseconds(float value) => TimeSpan.FromMilliseconds(value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TimeSpanF FromMilliseconds(double value) => TimeSpan.FromMilliseconds(value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TimeSpanF FromMinutes(float value) => TimeSpan.FromMinutes(value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TimeSpanF FromMinutes(double value) => TimeSpan.FromMinutes(value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TimeSpanF FromSeconds(float value) => TimeSpan.FromSeconds(value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TimeSpanF FromSeconds(double value) => TimeSpan.FromSeconds(value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TimeSpanF FromTicks(long value) => TimeSpan.FromTicks(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TimeSpanF Parse(string input, IFormatProvider? formatProvider) => TimeSpan.Parse(input, formatProvider);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TimeSpanF Parse(string s) => TimeSpan.Parse(s);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TimeSpanF Parse(ReadOnlySpan<char> input, IFormatProvider? formatProvider = null) => TimeSpan.Parse(input, formatProvider);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TimeSpanF ParseExact(string input, string[] formats, IFormatProvider? formatProvider, TimeSpanStyles styles) => TimeSpan.ParseExact(input, formats, formatProvider);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TimeSpanF ParseExact(string input, string format, IFormatProvider? formatProvider, TimeSpanStyles styles) => TimeSpan.ParseExact(input, format, formatProvider);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TimeSpanF ParseExact(string input, string format, IFormatProvider? formatProvider) => TimeSpan.ParseExact(input, format, formatProvider);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TimeSpanF ParseExact(ReadOnlySpan<char> input, string[] formats, IFormatProvider? formatProvider, TimeSpanStyles styles = TimeSpanStyles.None) => TimeSpan.ParseExact(input, formats, formatProvider);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TimeSpanF ParseExact(ReadOnlySpan<char> input, ReadOnlySpan<char> format, IFormatProvider? formatProvider, TimeSpanStyles styles = TimeSpanStyles.None) => TimeSpan.ParseExact(input, format, formatProvider, styles);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TimeSpanF ParseExact(string input, string[] formats, IFormatProvider? formatProvider) => TimeSpan.ParseExact(input, formats, formatProvider);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryParse(ReadOnlySpan<char> s, out TimeSpanF result)
        {
            return TimeSpan.TryParse(s, out OutAsTimeSpanF(out result));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryParse([NotNullWhen(true)] string? input, IFormatProvider? formatProvider, out TimeSpanF result)
        {
            return TimeSpan.TryParse(input, formatProvider, out OutAsTimeSpanF(out result));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryParse([NotNullWhen(true)] string? s, out TimeSpanF result)
        {
            return TimeSpan.TryParse(s, out OutAsTimeSpanF(out result));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryParse(ReadOnlySpan<char> input, IFormatProvider? formatProvider, out TimeSpanF result)
        {
            return TimeSpan.TryParse(input, formatProvider, out OutAsTimeSpanF(out result));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryParseExact([NotNullWhen(true)] string? input, [NotNullWhen(true)] string? format, IFormatProvider? formatProvider, TimeSpanStyles styles, out TimeSpanF result)
        {
            return TimeSpan.TryParseExact(input, format, formatProvider, styles, out OutAsTimeSpanF(out result));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryParseExact(ReadOnlySpan<char> input, [NotNullWhen(true)] string?[]? formats, IFormatProvider? formatProvider, out TimeSpanF result)
        {
            return TimeSpan.TryParseExact(input, formats, formatProvider, out OutAsTimeSpanF(out result));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryParseExact(ReadOnlySpan<char> input, [NotNullWhen(true)] string?[]? formats, IFormatProvider? formatProvider, TimeSpanStyles styles, out TimeSpanF result)
        {
            return TimeSpan.TryParseExact(input, formats, formatProvider, styles, out OutAsTimeSpanF(out result));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryParseExact(ReadOnlySpan<char> input, ReadOnlySpan<char> format, IFormatProvider? formatProvider, out TimeSpanF result)
        {
            return TimeSpan.TryParseExact(input, format, formatProvider, out OutAsTimeSpanF(out result));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryParseExact(ReadOnlySpan<char> input, ReadOnlySpan<char> format, IFormatProvider? formatProvider, TimeSpanStyles styles, out TimeSpanF result)
        {
            return TimeSpan.TryParseExact(input, format, formatProvider, styles, out OutAsTimeSpanF(out result));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryParseExact([NotNullWhen(true)] string? input, [NotNullWhen(true)] string?[]? formats, IFormatProvider? formatProvider, TimeSpanStyles styles, out TimeSpanF result)
        {
            return TimeSpan.TryParseExact(input, formats, formatProvider, styles, out OutAsTimeSpanF(out result));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryParseExact([NotNullWhen(true)] string? input, [NotNullWhen(true)] string?[]? formats, IFormatProvider? formatProvider, out TimeSpanF result)
        {
            return TimeSpan.TryParseExact(input, formats, formatProvider, out OutAsTimeSpanF(out result));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryParseExact([NotNullWhen(true)] string? input, [NotNullWhen(true)] string? format, IFormatProvider? formatProvider, out TimeSpanF result)
        {
            return TimeSpan.TryParseExact(input, format, formatProvider, out OutAsTimeSpanF(out result));
        }
    }
}
