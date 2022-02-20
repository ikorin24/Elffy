#nullable enable
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Elffy
{
    /// <summary>It is similar to <see cref="Func{T, TResult}"/> which contains the state in itself.</summary>
    /// <remarks>Function delegate whose argument is strong-typed but non generic.</remarks>
    /// <typeparam name="TResult">returned type from the delegate</typeparam>
    public unsafe readonly struct StatefulFunc<TResult> : IEquatable<StatefulFunc<TResult>>
    {
        private readonly delegate*<Delegate, object?, TResult> _funcCaller;
        private readonly Delegate _func;
        private readonly object? _state;

        public static StatefulFunc<TResult> None => default;

        public bool IsNone => _funcCaller == null;

        [Obsolete("Don't use defaut constructor.", true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public StatefulFunc() => throw new NotSupportedException("Don't use defaut constructor.");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private StatefulFunc(object? state, Delegate func, delegate*<Delegate, object?, TResult> funcCaller)
        {
            Debug.Assert(funcCaller != null);
            Debug.Assert(func is not null);
            _state = state;
            _func = func;
            _funcCaller = funcCaller;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static StatefulFunc<TResult> Create<TState>(TState state, Func<TState, TResult> func) where TState : class
        {
            if(func is null) {
                ThrowNullArg();
                [DoesNotReturn] static void ThrowNullArg() => throw new ArgumentNullException(nameof(func));
            }
            return new StatefulFunc<TResult>(state, func, &FuncCaller);

            [MethodImpl(MethodImplOptions.AggressiveOptimization)]
            static TResult FuncCaller(Delegate func, object? state)
            {
                var s = SafeCast.As<TState>(state);
                var f = SafeCast.As<Func<TState?, TResult>>(func);
                return f.Invoke(s);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static StatefulFunc<TResult> Create(Func<TResult> func)
        {
            if(func is null) {
                ThrowNullArg();
                [DoesNotReturn] static void ThrowNullArg() => throw new ArgumentNullException(nameof(func));
            }
            return new StatefulFunc<TResult>(null, func, &FuncCaller);

            [MethodImpl(MethodImplOptions.AggressiveOptimization)]
            static TResult FuncCaller(Delegate func, object? _)
            {
                return SafeCast.As<Func<TResult>>(func).Invoke();
            }
        }

        public TResult Invoke() => _funcCaller(_func, _state);

        public override bool Equals(object? obj) => obj is StatefulFunc<TResult> func && Equals(func);

        public bool Equals(StatefulFunc<TResult> other)
        {
            // [NOTE]
            // Equality of function pointers is not comparable because of tiered compilation.

            return ReferenceEquals(_func, other._func) && ReferenceEquals(_state, other._state);
        }

        public override int GetHashCode() => HashCode.Combine(_func, _state);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(in StatefulFunc<TResult> left, in StatefulFunc<TResult> right) => left.Equals(right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(in StatefulFunc<TResult> left, in StatefulFunc<TResult> right) => !(left == right);
    }

    /// <summary>Provides helper methods for <see cref="StatefulFunc{TResult}"/></summary>
    public static class StatefulFunc
    {
        public static StatefulFunc<TResult> None<TResult>() => StatefulFunc<TResult>.None;

        /// <summary>Create <see cref="StatefulFunc{TResult}"/> from a state and <see cref="Func{T, TResult}"/>.</summary>
        /// <typeparam name="TState">type of the state</typeparam>
        /// <typeparam name="TResult">type of the result</typeparam>
        /// <param name="state">state object</param>
        /// <param name="func">function delegate whose arg is <paramref name="state"/> that returns a result of type <typeparamref name="TResult"/></param>
        /// <returns><see cref="StatefulFunc{TResult}"/> instance</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StatefulFunc<TResult> Create<TState, TResult>(TState state, Func<TState, TResult> func) where TState : class
        {
            return StatefulFunc<TResult>.Create(state, func);
        }

        /// <summary>Create <see cref="StatefulFunc{TResult}"/> from <see cref="Func{TResult}"/> without a state.</summary>
        /// <typeparam name="TResult">type of the result</typeparam>
        /// <param name="func">function delegate that returns a result of type <typeparamref name="TResult"/></param>
        /// <returns><see cref="StatefulFunc{TResult}"/> instance</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StatefulFunc<TResult> Create<TResult>(Func<TResult> func)
        {
            return StatefulFunc<TResult>.Create(func);
        }
    }
}
