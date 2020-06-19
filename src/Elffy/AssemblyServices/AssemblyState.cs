#nullable enable

using System.IO;
using System.Reflection;

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

        /// <summary>アプリケーションドメインのエントリーアセンブリ (exe) のパスを取得します。</summary>
        /// <remarks>アンマネージコードからのエントリーの場合 null を返します</remarks>
        public static string EntryAssemblyPath => _entryAssemblyPath ??= Assembly.GetEntryAssembly()?.Location!;
        private static string? _entryAssemblyPath;

        /// <summary>アプリケーションドメインのエントリーアセンブリ (exe) のあるディレクトリを取得します。</summary>
        /// <remarks>アンマネージコードからのエントリーの場合 null を返します</remarks>
        public static string EntryAssemblyDirectory => _entryAssemblyDirectory ??= Path.GetDirectoryName(EntryAssemblyPath)!;
        private static string? _entryAssemblyDirectory;
    }
}
