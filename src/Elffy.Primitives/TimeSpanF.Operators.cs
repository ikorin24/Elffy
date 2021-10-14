#nullable enable
using System;
using System.Runtime.CompilerServices;

namespace Elffy
{
    partial struct TimeSpanF
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TimeSpanF Add(TimeSpanF ts) => _ts.Add(ts);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TimeSpanF Add(TimeSpan ts) => _ts.Add(ts);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TimeSpanF Divide(float divisor) => _ts.Divide(divisor);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TimeSpanF Divide(double divisor) => _ts.Divide(divisor);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Divide(TimeSpanF ts) => (float)_ts.Divide(ts);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Divide(TimeSpan ts) => (float)_ts.Divide(ts);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TimeSpanF Duration() => _ts.Duration();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TimeSpanF Multiply(float factor) => _ts.Multiply(factor);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TimeSpanF Multiply(double factor) => _ts.Multiply(factor);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TimeSpanF Negate() => _ts.Negate();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TimeSpanF Subtract(TimeSpanF ts) => _ts.Subtract(ts);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TimeSpanF Subtract(TimeSpan ts) => _ts.Subtract(ts);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TimeSpanF operator +(TimeSpanF t) => t._ts;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TimeSpanF operator +(TimeSpanF t1, TimeSpanF t2) => t1._ts + t2._ts;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TimeSpanF operator +(TimeSpan t1, TimeSpanF t2) => t1 + t2._ts;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TimeSpanF operator +(TimeSpanF t1, TimeSpan t2) => t1._ts + t2;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TimeSpanF operator -(TimeSpanF t) => -t._ts;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TimeSpanF operator -(TimeSpanF t1, TimeSpanF t2) => t1._ts - t2._ts;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TimeSpanF operator -(TimeSpan t1, TimeSpanF t2) => t1 - t2._ts;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TimeSpanF operator -(TimeSpanF t1, TimeSpan t2) => t1._ts - t2;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TimeSpanF operator *(float factor, TimeSpanF timeSpan) => factor * timeSpan._ts;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TimeSpanF operator *(double factor, TimeSpanF timeSpan) => factor * timeSpan._ts;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TimeSpanF operator *(TimeSpanF timeSpan, float factor) => timeSpan._ts * factor;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TimeSpanF operator *(TimeSpanF timeSpan, double factor) => timeSpan._ts * factor;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TimeSpanF operator /(TimeSpanF timeSpan, float divisor) => timeSpan._ts / divisor;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TimeSpanF operator /(TimeSpanF timeSpan, double divisor) => timeSpan._ts / divisor;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float operator /(TimeSpanF t1, TimeSpanF t2) => (float)(t1._ts / t2._ts);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float operator /(TimeSpan t1, TimeSpanF t2) => (float)(t1 / t2._ts);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float operator /(TimeSpanF t1, TimeSpan t2) => (float)(t1._ts / t2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(TimeSpanF left, TimeSpanF right) => left.Equals(right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(TimeSpanF left, TimeSpanF right) => !(left == right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(TimeSpanF left, TimeSpan right) => left._ts == right;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(TimeSpanF left, TimeSpan right) => !(left == right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(TimeSpan left, TimeSpanF right) => left == right._ts;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(TimeSpan left, TimeSpanF right) => !(left == right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <(TimeSpanF t1, TimeSpanF t2) => t1._ts < t2._ts;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <(TimeSpanF t1, TimeSpan t2) => t1._ts < t2;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <(TimeSpan t1, TimeSpanF t2) => t1 < t2._ts;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(TimeSpanF t1, TimeSpanF t2) => t1._ts > t2._ts;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(TimeSpanF t1, TimeSpan t2) => t1._ts > t2;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(TimeSpan t1, TimeSpanF t2) => t1 > t2._ts;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <=(TimeSpanF t1, TimeSpanF t2) => t1._ts <= t2._ts;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <=(TimeSpanF t1, TimeSpan t2) => t1._ts <= t2;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <=(TimeSpan t1, TimeSpanF t2) => t1 <= t2._ts;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >=(TimeSpanF t1, TimeSpanF t2) => t1._ts >= t2._ts;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >=(TimeSpanF t1, TimeSpan t2) => t1._ts >= t2;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >=(TimeSpan t1, TimeSpanF t2) => t1 >= t2._ts;
    }
}
