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
            return source ?? string.Intern(source);
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
    }
}
