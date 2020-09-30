#nullable enable

using System.IO;
using System.Reflection;

namespace Elffy.AssemblyServices
{
    /// <summary>アセンブリの状態を提供するクラス</summary>
    public static class AssemblyState
    {
        /// <summary>Get whether this assembly is compiled with symbol of '#define DEBUG'.</summary>
        public const bool IsDebug =
#if DEBUG
            true;
#else
            false;
#endif

        /// <summary>Get whether this assembly is compiled with symbol of '#define DEVELOP'.</summary>
        public const bool IsDevelop =
#if DEVELOP
            true;
#else
            false;
#endif
        internal const string Symbol_DEBUG = "DEBUG";
        internal const string Symbol_Develop = "DEVELOP";

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
