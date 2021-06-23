#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace Elffy.Effective
{
    internal static class ThrowHelper
    {
        [DoesNotReturn]
        public static void NullArg(string message) => throw new ArgumentNullException(message);

        [DoesNotReturn]
        public static void ArgOutOfRange(string message) => throw new ArgumentOutOfRangeException(message);

        [DoesNotReturn]
        public static void Arg(string message) => throw new ArgumentException(message);

        [DoesNotReturn]
        public static void InvalidOperation(string message) => throw new InvalidOperationException(message);

        [DoesNotReturn]
        public static void KeyNotFound<T>(T key) => throw new KeyNotFoundException($"key: {key}");

        [DoesNotReturn]
        public static void Arg_KeyDuplicated<T>(T key) => throw new ArgumentException($"The key is duplicated. key: {key}");

        [DoesNotReturn]
        public static void InvalidOperation_ConcurrentOperationsNotSupported()
            => throw new InvalidOperationException("Operations that change non-concurrent collections must have exclusive access. A concurrent update was performed on this collection and corrupted its state. The collection's state is no longer correct.");

        [DoesNotReturn]
        public static void Serialization_MissingKeys()
        {
            throw new SerializationException("The Keys for this Hashtable are missing.");
        }

        [DoesNotReturn]
        public static void Serialization_NullKey()
        {
            throw new SerializationException("One of the serialized keys is null.");
        }

        [DoesNotReturn]
        public static void InvalidOperation_ModifiedOnEnumerating() => throw new InvalidOperationException("Collection was modified; enumeration operation may not execute.");
    }
}
