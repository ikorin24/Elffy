#nullable enable

#if !NETCOREAPP3_1
#define CAN_SKIP_LOCALS_INIT
#endif

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Elffy.Effective.Unsafes
{
    /// <summary>Helper class of <see cref="Unsafe"/></summary>
    public static class UnsafeEx
    {
        /// <summary>
        /// Casts given readonly reference to a readonly reference to a value of type <typeparamref name="TTo"/>.
        /// </summary>
        /// <remarks>This is compatible with `ref TTo Unsafe.As&lt;TFrom, TTo&gt;(ref TFrom)`</remarks>
        /// <typeparam name="TFrom">source type</typeparam>
        /// <typeparam name="TTo">destination type</typeparam>
        /// <param name="source">source reference</param>
        /// <returns>readonly reference to a value of type <typeparamref name="TTo"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref readonly TTo As<TFrom, TTo>(in TFrom source)
        {
            return ref Unsafe.As<TFrom, TTo>(ref Unsafe.AsRef(source));
        }

        /// <summary>Determines whether the specified references point to the same location.</summary>
        /// <typeparam name="T">The type of reference.</typeparam>
        /// <param name="left">The first reference to compare.</param>
        /// <param name="right">The second reference to compare.</param>
        /// <returns>true if left and right point to the same location; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AreSame<T>(in T left, in T right)
        {
            return Unsafe.AreSame(ref Unsafe.AsRef(left), ref Unsafe.AsRef(right));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void* AsPointer<T>(in T value)
        {
            return Unsafe.AsPointer(ref Unsafe.AsRef(in value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SkipInitIfPossible<T>(out T value)
        {
#if CAN_SKIP_LOCALS_INIT
            Unsafe.SkipInit(out value);
#else
            value = default!;
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static ref T NullRef<T>()
        {
            return ref Unsafe.AsRef<T>(null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static bool IsNullRef<T>(ref T source)
        {
            return Unsafe.AreSame(ref source, ref Unsafe.AsRef<T>(null));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Span<byte> AsBytes<T>(ref T value) where T : unmanaged
        {
            return MemoryMarshal.CreateSpan(ref Unsafe.As<T, byte>(ref value), sizeof(T));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe ReadOnlySpan<byte> AsReadOnlyBytes<T>(in T value) where T : unmanaged
        {
            return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, byte>(ref Unsafe.AsRef(in value)), sizeof(T));
        }
    }
}
