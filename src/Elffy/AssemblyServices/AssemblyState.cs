#nullable enable

namespace Elffy.AssemblyServices
{
    /// <summary>アセンブリの状態を提供するクラス</summary>
    public static class AssemblyState
    {
        // ** NOTE **
        // このクラスが提供する条件付きコンパイルのフラグ API はライブラリ利用者向けです。
        // ライブラリ内ではプリプロセッサディレクティブを使ってください

        /// <summary>現在のアセンブリが条件付きコンパイル #define CHECK_ARG でコンパイルされたかどうかを取得します</summary>
        /// <returns>条件付きコンパイルで CHECK_ARG が定義済みであるかどうか</returns>
        public static bool IsArgumentChecked()
        {
#if CHECK_ARG
            return true;
#else
            return false;
#endif
        }

        /// <summary>現在のアセンブリが条件付きコンパイル #define DEBUG でコンパイルされたかどうかを取得します</summary>
        /// <returns>条件付きコンパイルで DEBUG が定義済みであるかどうか</returns>
        public static bool IsDebug()
        {
#if DEBUG
            return true;
#else
            return false;
#endif
        }
    }
}
