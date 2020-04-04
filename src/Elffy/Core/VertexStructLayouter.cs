#nullable enable
using System;
using Elffy.Exceptions;

namespace Elffy.Core
{
    public static class VertexStructLayouter<T> where T : unmanaged
    {
        private static Action? _layouter;
        internal static Action Layouter => _layouter ?? throw new InvalidOperationException($"'{typeof(T).FullName}' has no layouter method.");

        /// <summary>
        /// 頂点を表す構造体のメモリレイアウトをOpenGLに伝えるメソッドをセットします。<para/>
        /// 通常、対象の構造体の static コンストラクタ内から呼んでください。<para/>
        /// </summary>
        /// <param name="layouter">メモリレイアウトを伝えるメソッド</param>
        public static void SetLayouter(Action layouter)
        {
            ArgumentChecker.ThrowIfNullArg(layouter, nameof(layouter));
            if(_layouter != null) { throw new InvalidOperationException("Layouter is already set"); }
            _layouter = layouter;
        }
    }
}
