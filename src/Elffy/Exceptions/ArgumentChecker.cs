using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Elffy.Exceptions
{
    /// <summary>引数確認で例外を投げるためのクラス。条件付きコンパイルで制御できます</summary>
    internal static class ArgumentChecker
    {
        private const string CHECK_ARG = "CHECK_ARG";

        /// <summary>指定の引数がnullの場合 <see cref="ArgumentNullException"/> を投げます</summary>
        /// <typeparam name="T">引数の型</typeparam>
        /// <param name="arg">nullチェックする引数</param>
        /// <param name="paramName">引数の名前</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional(CHECK_ARG)]
        public static void ThrowIfNullArg<T>(T arg, string paramName)
        {
            if(arg == null) { throw new ArgumentNullException(paramName); }
        }

        /// <summary>条件がtrueなら指定した例外を投げます</summary>
        /// <param name="condition">例外を投げる条件</param>
        /// <param name="ex">投げる例外</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional(CHECK_ARG)]
        public static void ThrowIf(bool condition, Exception ex)
        {
            if(condition) { throw ex; }
        }

        /// <summary>辞書から指定のキーの要素を取得します。キーが存在しない場合は指定の例外を投げます</summary>
        /// <typeparam name="TKey">キーの型</typeparam>
        /// <typeparam name="TValue">要素の型</typeparam>
        /// <param name="dic">辞書</param>
        /// <param name="key">キー</param>
        /// <param name="ex">例外</param>
        /// <returns>取得した要素</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TValue GetDicValue<TKey, TValue>(IDictionary<TKey, TValue> dic, TKey key, Exception ex)
        {
#if CHECK_ARG
            if(!dic.TryGetValue(key, out var value)) {
                throw ex;
            }
            return value;
#else
            return dic[key];
#endif
        }
    }
}
