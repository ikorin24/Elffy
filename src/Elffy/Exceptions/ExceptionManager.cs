using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Elffy.Exceptions
{
    /// <summary>引数確認で例外を投げるためのクラス。条件付きコンパイルで制御できます</summary>
    internal static class ExceptionManager
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
