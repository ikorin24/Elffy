#nullable enable

using System;

namespace Elffy.Effective
{
    /// <summary><see cref="string"/> 型の拡張メソッド用クラス</summary>
    public static class StringExtension
    {
        /// <summary>指定の文字列をインターンプールに登録済みの文字列として、その参照を取得します</summary>
        /// <param name="source">文字列</param>
        /// <returns>インターンプールの文字列参照</returns>
        public static string AsInterned(this string source)
        {
            return (source is null) ? source! : string.Intern(source);
        }

        /// <summary>指定の文字列が null または空文字列かどうかを取得します。(<see cref="string.IsNullOrEmpty(string)"/> への拡張メソッド)</summary>
        /// <param name="source">文字列</param>
        /// <returns>指定の文字列が null または空文字列かどうか</returns>
        public static bool IsNullOrEmpty(this string source)
        {
            return string.IsNullOrEmpty(source);
        }

        /// <summary>指定の文字列が null または空白文字列かどうかを取得します。(<see cref="string.IsNullOrWhiteSpace(string)"/> への拡張メソッド)</summary>
        /// <param name="source">文字列</param>
        /// <returns>指定の文字列が null または空白文字列かどうか</returns>
        public static bool IsNullOrWhiteSpace(this string source)
        {
            return string.IsNullOrWhiteSpace(source);
        }

        /// <summary>ファイルパスの拡張子を部分文字列で取得します。(ドットを含みます (ex) ".png" )</summary>
        /// <param name="source">文字列</param>
        /// <returns>拡張子</returns>
        public static ReadOnlySpan<char> FilePathExtension(this string source)
        {
            if(source is null) { throw new ArgumentNullException(nameof(source)); }
            for(int i = source.Length - 1; i >= 0; i--) {
                if(source[i] == '.') {
                    return source.AsSpan(i);
                }
            }
            return ReadOnlySpan<char>.Empty;
        }
    }
}
