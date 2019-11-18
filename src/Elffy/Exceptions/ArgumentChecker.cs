#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

namespace Elffy.Exceptions
{
    /// <summary>Argument checker class. Switch enable/disable checking by conditional compiler.</summary>
    internal static class ArgumentChecker
    {
        /// <summary>Symbol of conditional compiling</summary>
        private const string CHECK_ARG = "CHECK_ARG";

        /// <summary>Throw <see cref="ArgumentNullException"/> if specified argument is null.</summary>
        /// <typeparam name="T">type of argument</typeparam>
        /// <param name="arg">null-checked argument</param>
        /// <param name="paramName">argument name</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional(CHECK_ARG)]
        public static void ThrowIfNullArg<T>(T arg, string paramName) where T : class
        {
            if(arg == null) { throw new ArgumentNullException(paramName); }
        }

        /// <summary>Throw <see cref="ArgumentException"/> if <paramref name="condition"/> is true.</summary>
        /// <param name="condition">condition where exception is thrown</param>
        /// <param name="message">message of exception</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional(CHECK_ARG)]
        public static void ThrowArgumentIf(bool condition, string message)
        {
            if(condition) {
                throw new ArgumentException(message);
            }
        }

        /// <summary>Throw <see cref="ArgumentOutOfRangeException"/> if <paramref name="condition"/> is true.</summary>
        /// <typeparam name="TValue">type of value</typeparam>
        /// <param name="condition">condition where exception is thrown</param>
        /// <param name="paramName">name of parameter</param>
        /// <param name="actualValue">actual value</param>
        /// <param name="message">message of exception</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional(CHECK_ARG)]
        public static void ThrowOutOfRangeIf<TValue>(bool condition, string paramName, TValue actualValue, string message) where TValue : struct
        {
            if(condition) {
                throw new ArgumentOutOfRangeException(paramName, actualValue, message);
            }
        }

        /// <summary>Throw <see cref="FileNotFoundException"/> if <paramref name="condition"/> is true.</summary>
        /// <param name="condition">condition where exception is thrown</param>
        /// <param name="message">message of exception</param>
        /// <param name="filename">file name</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional(CHECK_ARG)]
        public static void ThrowFileNotFoundIf(bool condition, string message, string filename)
        {
            if(condition) {
                throw new FileNotFoundException(message, filename);
            }
        }

        /// <summary>辞書から指定のキーの要素を取得します。キーが存在しない場合はキーの値と指定のメッセージをメッセージに持った例外を投げます</summary>
        /// <typeparam name="TKey">キーの型</typeparam>
        /// <typeparam name="TValue">要素の型</typeparam>
        /// <param name="dic">辞書</param>
        /// <param name="key">キー</param>
        /// <param name="message">メッセージ</param>
        /// <returns>取得した要素</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TValue GetValueWithKeyChecking<TKey, TValue>(this Dictionary<TKey, TValue> dic, TKey key, string message)
        {
            // ** NOTE **
            // For method inlining, the type of dic is Dictionary, NOT IDictionary.
            // DO NOT change IDictionary.

#if CHECK_ARG
            if(!dic.TryGetValue(key, out var value)) {
                throw new KeyNotFoundException($"Key : {key}; {message}");
            }
            return value;
#else
            return dic[key];
#endif
        }

        /// <summary>指定の型にキャスト不可である場合、例外を投げます</summary>
        /// <typeparam name="TDesired">希望する型</typeparam>
        /// <typeparam name="TData">データの型</typeparam>
        /// <param name="arg">データ</param>
        /// <param name="paramName">引数の名前</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional(CHECK_ARG)]
        public static void CheckType<TDesired, TData>(TData arg, string paramName)
        {
            if(arg is TDesired == false) { throw new InvalidCastException(paramName); }
        }
    }
}
