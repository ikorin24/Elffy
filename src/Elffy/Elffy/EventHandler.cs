using System;
using System.Collections.Generic;

namespace Elffy
{
    /// <summary>delegate of generic-type sender</summary>
    /// <typeparam name="T">type of sender</typeparam>
    /// <param name="sender">the sender that fires this delegate</param>
    public delegate void ActionEventHandler<T>(T sender);

    /// <summary>delegate of generic-type sender and generic type argument</summary>
    /// <typeparam name="T">type of sender</typeparam>
    /// <typeparam name="TArg">type of argument</typeparam>
    /// <param name="sender">the sender that fires this delegate</param>
    /// <param name="e">the argument of this delegate</param>
    public delegate void ActionEventHandler<T, TArg>(T sender, TArg e);

    /// <summary>event args of value changed</summary>
    /// <typeparam name="T">type of values</typeparam>
    public struct ValueChangedEventArgs<T> : IEquatable<ValueChangedEventArgs<T>>
    {
        /// <summary>old value of the value changed event</summary>
        public T OldValue { get; private set; }
        /// <summary>new value of the value changed event</summary>
        public T NewValue { get; private set; }

        /// <summary>constructor of <see cref="ValueChangedEventArgs{T}"/></summary>
        /// <param name="oldValue">old value of the value changed event</param>
        /// <param name="newValue">new value of the value changed event</param>
        public ValueChangedEventArgs(T oldValue, T newValue)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }

        public override bool Equals(object obj)
        {
            return obj is ValueChangedEventArgs<T> args && Equals(args);
        }

        public bool Equals(ValueChangedEventArgs<T> other)
        {
            return EqualityComparer<T>.Default.Equals(OldValue, other.OldValue) &&
                   EqualityComparer<T>.Default.Equals(NewValue, other.NewValue);
        }

        public override int GetHashCode()
        {
            var hashCode = -279159539;
            hashCode = hashCode * -1521134295 + EqualityComparer<T>.Default.GetHashCode(OldValue);
            hashCode = hashCode * -1521134295 + EqualityComparer<T>.Default.GetHashCode(NewValue);
            return hashCode;
        }

        public static bool operator ==(ValueChangedEventArgs<T> left, ValueChangedEventArgs<T> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ValueChangedEventArgs<T> left, ValueChangedEventArgs<T> right)
        {
            return !(left == right);
        }
    }
}
