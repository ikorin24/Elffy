#nullable enable
using System;
using System.Runtime.CompilerServices;

namespace Elffy
{
    /// <summary>
    /// Represents a time interval. It is compatible with <see cref="TimeSpan"/>.
    /// </summary>
    [Serializable]
    public readonly partial struct TimeSpanF : IEquatable<TimeSpanF>, IComparable<TimeSpanF>, IComparable, IFormattable
    {
        // TODO: Generic math implementation for .NET6

        private readonly TimeSpan _ts;

        public const long TicksPerDay = TimeSpan.TicksPerDay;
        public const long TicksPerHour = TimeSpan.TicksPerHour;
        public const long TicksPerMillisecond = TimeSpan.TicksPerMillisecond;
        public const long TicksPerMinute = TimeSpan.TicksPerMinute;
        public const long TicksPerSecond = TimeSpan.TicksPerSecond;

        public static readonly TimeSpanF MaxValue = TimeSpan.MaxValue;
        public static readonly TimeSpanF MinValue = TimeSpan.MinValue;
        public static readonly TimeSpanF Zero = TimeSpan.Zero;

        public float TotalMilliseconds => (float)_ts.TotalMilliseconds;
        public float TotalHours => (float)_ts.TotalHours;
        public float TotalDays => (float)_ts.TotalDays;
        public double TotalMinutes => (float)_ts.TotalMinutes;
        public double TotalSeconds => (float)_ts.TotalSeconds;

        public long Ticks => _ts.Ticks;
        public int Seconds => _ts.Seconds;
        public int Minutes => _ts.Minutes;
        public int Milliseconds => _ts.Milliseconds;
        public int Hours => _ts.Hours;
        public int Days => _ts.Days;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TimeSpanF(TimeSpan timeSpan) => _ts = timeSpan;

        public TimeSpanF(long ticks) : this(new TimeSpan(ticks)) { }

        public TimeSpanF(int hours, int minutes, int seconds) : this(new TimeSpan(hours, minutes, seconds)) { }

        public TimeSpanF(int days, int hours, int minutes, int seconds) : this(new TimeSpan(days, hours, minutes, seconds)) { }

        public TimeSpanF(int days, int hours, int minutes, int seconds, int milliseconds) : this(new TimeSpan(days, hours, minutes, seconds, milliseconds)) { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format = default, IFormatProvider? formatProvider = null) => _ts.TryFormat(destination, out charsWritten, format, formatProvider);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(TimeSpanF other) => _ts.CompareTo(other._ts);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(object? obj) => _ts.CompareTo(obj);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object? obj) => obj is TimeSpanF f && Equals(f);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(TimeSpanF other) => _ts.Equals(other._ts);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => _ts.GetHashCode();

        public override string ToString() => _ts.ToString();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ToString(string? format) => _ts.ToString(format);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ToString(string? format, IFormatProvider? formatProvider) => _ts.ToString(format, formatProvider);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TimeSpan ToTimeSpan() => _ts;
    }

    public static class TimeSpanExtension
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TimeSpanF ToTimeSpanF(this TimeSpan timeSpan) => new TimeSpanF(timeSpan);
    }
}
