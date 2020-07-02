#nullable enable

#if NETFRAMEWORK || (NETCOREAPP && (NETCOREAPP1_0 || NETCOREAPP1_1 || NETCOREAPP2_0))
#define SLOW_SPAN   // .net framework or .net core before 2.0
#elif NETCOREAPP
#define FAST_SPAN   // .net core after 2.1
#endif

using Elffy.AssemblyServices;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using nint = System.IntPtr;

namespace Elffy.Effective.Unsafes
{
    internal static class SpanCastUnsafe
    {
        /// <summary>
        /// 参照型の <see cref="Span{T}"/> を別の型の <see cref="Span{T}"/> へキャストします。
        /// ※ 参照型かつ実行時型が壊れないようにする必要があります。必ず実装コメントを読んでください。
        /// </summary>
        /// <typeparam name="TFrom">変換元の参照型</typeparam>
        /// <typeparam name="TTo">変換先の参照型</typeparam>
        /// <param name="span">型変換を行う <see cref="Span{T}"/></param>
        /// <returns>変換後の <see cref="Span{T}"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CriticalDotnetDependency("netcoreapp3.1")]
        public static unsafe Span<TTo> CastRefType<TFrom, TTo>(Span<TFrom> span)
        {
            // [NOTE]
            // このメソッドは、呼び出し側が正しい型を指定しなければなりません。
            // 呼び出し側の表現力を制限しないために、あえて静的制限を設定していません。
            // 正しい型 (ともに参照型かつ継承関係にある型) を指定した場合、GC の追跡は壊れません。
            // 
            // 本来は where TFrom : class と where TTo : class で静的制限をかけるべきですが、
            // 呼び出し側が型制限なしのジェネリックの場合、特定の型に特殊化できないため実行時制限にしています。
            // つまり、呼び出し側が T に制限がない SomeGenericsType<T> で、
            // 
            // Span<object> objSpan = new object[5].AsSpan();
            // if(typeof(T).IsClass) {
            //     Span<T> tSpan = SpanCastUnsafe.CastRefType<object, T>(objSpan);
            // }
            // 
            // が行えるようにしています。
            //
            // また同様に、表現力を制限しないために、継承関係の制限もしていません。
            // このため、ソースとなる Span<T> に含まれる全ての要素の実行時型に注意する必要があります。
            // 例えば、object から string への共変方向への変換
            // SpanCastUnsafe.CastRefType<object, string>(objSpan) は一見安全に見えますが、
            // 
            // Span<object> objSpan = new object[5].AsSpan();
            // objSpan[0] = new SomeClass();           // SomeClass is not string
            // Span<string> stringSpan = SpanCastUnsafe.CastRefType<object, string>(objSpan);
            // string element0 = stringSpan[0];        // Runtime type is dengerous !!!!!
            //
            // のように実行時型が静的型と一致しなくなり、ランタイムがクラッシュする可能性があります。
            // ソースとなる Span<T> 内の全ての要素が null の場合は安全です。

            Debug.Assert(typeof(TFrom).IsClass);
            Debug.Assert(typeof(TTo).IsClass);

#if SLOW_SPAN
            return CastRefTypeForSlowSpan<TFrom, TTo>(span);
#endif
#if FAST_SPAN
            return CastRefTypeForFastSpan<TFrom, TTo>(span);
#endif
        }

#if SLOW_SPAN
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe Span<TTo> CastRefTypeForSlowSpan<TFrom, TTo>(Span<TFrom> span)
        {
            var helper1 = new PointerHelper<TFrom>(span);
            var helper2 = new PointerHelper<TTo>();
            var p1 = (&helper1.Head) + 1;       // p1 = &helper1.Span;
            var p2 = (&helper2.Head) + 1;       // p2 = &helper2.Span;
            *p2 = *p1;                          // ret._pinnale = span._pinnable
            *(p2 + 1) = *(p1 + 1);              // ret._byteOffset = span._byteOffset;
            *(int*)(p2 + 2) = *(int*)(p1 + 2);  // ret._length = span._length;

            return helper2.Span;
        }
#endif

#if FAST_SPAN
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe Span<TTo> CastRefTypeForFastSpan<TFrom, TTo>(Span<TFrom> span)
        {
            var helper1 = new PointerHelper<TFrom>(span);
            var helper2 = new PointerHelper<TTo>();
            var p1 = (&helper1.Head) + 1;       // p1 = &helper1.Span;
            var p2 = (&helper2.Head) + 1;       // p2 = &helper2.Span;
            *p2 = *p1;                          // ret._pointer = span._pointer;
            *(int*)(p2 + 1) = *(int*)(p1 + 1);  // ret._length = span._length;
            return helper2.Span;
        }
#endif

        [StructLayout(LayoutKind.Sequential)]
        private unsafe readonly ref struct PointerHelper<T>
        {
            public readonly nint Head;          // アライメントにパディングが入っては困るので nint 型にしておく 
                                                // (ジェネリックを含むと StructLayout で Explicit にできない)
            public readonly Span<T> Span;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public PointerHelper(Span<T> span)
            {
                Head = default;
                Span = span;
            }
        }
    }
}
