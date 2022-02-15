#nullable enable
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Elffy
{
    /// <summary>It is similar to <see cref="Action{T}"/> which contains the state in itself.</summary>
    /// <remarks>Action delegate whose argument is strong-typed but non generic.</remarks>
    public unsafe readonly struct StatefulAction
    {
        private readonly delegate*<Delegate, object?, void> _actionCaller;
        private readonly Delegate _action;
        private readonly object? _state;

        public static StatefulAction None => default;

        public bool IsNone => _actionCaller == null;

        [Obsolete("Don't use defaut constructor.", true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public StatefulAction() => throw new NotSupportedException("Don't use defaut constructor.");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private StatefulAction(object? state, Delegate action, delegate*<Delegate, object?, void> actionCaller)
        {
            Debug.Assert(actionCaller != null);
            Debug.Assert(action is not null);
            _state = state;
            _action = action;
            _actionCaller = actionCaller;
        }

        /// <summary>Create <see cref="StatefulAction"/> from a state and <see cref="Action"/> without a state.</summary>
        /// <typeparam name="TState">type of the state</typeparam>
        /// <param name="state">state object</param>
        /// <param name="action">action delegate whose arg is <paramref name="state"/></param>
        /// <returns><see cref="StatefulAction"/> instance</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StatefulAction Create<TState>(TState state, Action<TState> action) where TState : class
        {
            if(action is null) {
                ThrowNullArg();
                [DoesNotReturn] static void ThrowNullArg() => throw new ArgumentNullException(nameof(action));
            }
            return new StatefulAction(state, action, &ActionCaller);

            [MethodImpl(MethodImplOptions.AggressiveOptimization)]
            static void ActionCaller(Delegate action, object? state)
            {
                var s = SafeCast.As<TState>(state);
                var f = SafeCast.As<Action<TState?>>(action);
                f.Invoke(s);
            }
        }

        /// <summary>Create <see cref="StatefulAction"/> from <see cref="Action"/> without a state.</summary>
        /// <param name="action">action delegate</param>
        /// <returns><see cref="StatefulAction"/> instance</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StatefulAction Create(Action action)
        {
            if(action is null) {
                ThrowNullArg();
                [DoesNotReturn] static void ThrowNullArg() => throw new ArgumentNullException(nameof(action));
            }
            return new StatefulAction(null, action, &ActionCaller);

            [MethodImpl(MethodImplOptions.AggressiveOptimization)]
            static void ActionCaller(Delegate action, object? _)
            {
                SafeCast.As<Action>(action).Invoke();
            }
        }

        public void Invoke() => _actionCaller(_action, _state);

        public override bool Equals(object? obj) => obj is StatefulAction action && Equals(action);

        public bool Equals(StatefulAction other)
        {
            // [NOTE]
            // Equality of function pointers is not comparable because of tiered compilation.

            return ReferenceEquals(_action, other._action) && ReferenceEquals(_state, other._state);
        }

        public override int GetHashCode() => HashCode.Combine(_action, _state);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(in StatefulAction left, in StatefulAction right) => left.Equals(right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(in StatefulAction left, in StatefulAction right) => !(left == right);
    }
}
