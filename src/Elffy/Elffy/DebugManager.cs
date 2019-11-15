#nullable enable
using System.Diagnostics;

namespace Elffy
{
    public static class DebugManager
    {
        private const string SPACE = "  ";
        private static bool _isAppended;

        #region Append
        /// <summary>デバッグ出力バッファに文字を追加します</summary>
        /// <param name="value">値</param>
        /// <param name="space">区切り文字(省略可)</param>
        [Conditional("DEBUG")]
        public static void Append(string value, string space = SPACE)
        {
            Debug.Write(value + SPACE);
            _isAppended = true;
        }

        /// <summary>デバッグ出力バッファに文字を追加します</summary>
        /// <param name="value">値</param>
        /// <param name="space">区切り文字(省略可)</param>
        [Conditional("DEBUG")]
        public static void Append(int value, string space = SPACE)
        {
            Debug.Write(value + SPACE);
            _isAppended = true;
        }

        /// <summary>デバッグ出力バッファに文字を追加します</summary>
        /// <param name="value">値</param>
        /// <param name="space">区切り文字(省略可)</param>
        [Conditional("DEBUG")]
        public static void Append(double value, string space = SPACE)
        {
            Debug.Write(value + SPACE);
            _isAppended = true;
        }

        /// <summary>デバッグ出力バッファに文字を追加します</summary>
        /// <param name="value">値</param>
        /// <param name="space">区切り文字(省略可)</param>
        [Conditional("DEBUG")]
        public static void Append(float value, string space = SPACE)
        {
            Debug.Write(value + SPACE);
            _isAppended = true;
        }

        /// <summary>デバッグ出力バッファに文字を追加します</summary>
        /// <param name="value">値</param>
        /// <param name="space">区切り文字(省略可)</param>
        [Conditional("DEBUG")]
        public static void Append(object value, string space = SPACE)
        {
            Debug.Write(value + SPACE);
            _isAppended = true;
        }
        #endregion

        #region AppendIf
        /// <summary>デバッグ出力バッファに文字を追加します</summary>
        /// <param name="condition">出力条件</param>
        /// <param name="value">値</param>
        /// <param name="space">区切り文字(省略可)</param>
        [Conditional("DEBUG")]
        public static void AppendIf(bool condition, string value, string space = SPACE)
        {
            if(condition) {
                Append(value, space);
            }
        }

        /// <summary>デバッグ出力バッファに文字を追加します</summary>
        /// <param name="condition">出力条件</param>
        /// <param name="value">値</param>
        /// <param name="space">区切り文字(省略可)</param>
        [Conditional("DEBUG")]
        public static void AppendIf(bool condition, int value, string space = SPACE)
        {
            if(condition) {
                Append(value, space);
            }
        }

        /// <summary>デバッグ出力バッファに文字を追加します</summary>
        /// <param name="condition">出力条件</param>
        /// <param name="value">値</param>
        /// <param name="space">区切り文字(省略可)</param>
        [Conditional("DEBUG")]
        public static void AppendIf(bool condition, double value, string space = SPACE)
        {
            if(condition) {
                Append(value, space);
            }
        }

        /// <summary>デバッグ出力バッファに文字を追加します</summary>
        /// <param name="condition">出力条件</param>
        /// <param name="value">値</param>
        /// <param name="space">区切り文字(省略可)</param>
        [Conditional("DEBUG")]
        public static void AppendIf(bool condition, float value, string space = SPACE)
        {
            if(condition) {
                Append(value, space);
            }
        }

        /// <summary>デバッグ出力バッファに文字を追加します</summary>
        /// <param name="condition">出力条件</param>
        /// <param name="value">値</param>
        /// <param name="space">区切り文字(省略可)</param>
        [Conditional("DEBUG")]
        public static void AppendIf(bool condition, object value, string space = SPACE)
        {
            if(condition) {
                Append(value, space);
            }
        }
        #endregion

        [Conditional("DEBUG")]
        internal static void Next()
        {
            if(_isAppended) {
                Debug.WriteLine("");
                _isAppended = false;
            }
        }
    }
}
