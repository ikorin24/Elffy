#nullable enable

namespace Elffy.AssemblyServices
{
    /// <summary>アセンブリの状態を提供するクラス</summary>
    public static class AssemblyState
    {
        /// <summary>現在のアセンブリが条件付きコンパイル #define CHECK_ARG でコンパイルされたかどうかを取得します</summary>
        /// <returns>条件付きコンパイルで CHECK_ARG が定義済みであるかどうか</returns>
        public const bool IsArgumentChecked =
#if CHECK_ARG
            true;
#else
            false;
#endif

        /// <summary>現在のアセンブリが条件付きコンパイル #define DEBUG でコンパイルされたかどうかを取得します</summary>
        /// <returns>条件付きコンパイルで DEBUG が定義済みであるかどうか</returns>
        public const bool IsDebug =
#if DEBUG
            true;
#else
            false;
#endif
    }
}
