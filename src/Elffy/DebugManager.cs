using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Elffy
{
    public static class DebugManager
    {
        private const string SPACE = "  ";
        private static StringBuilder _sb = new StringBuilder();

        #region Append
        /// <summary>デバッグ出力バッファに文字を追加します</summary>
        /// <param name="value">値</param>
        /// <param name="space">区切り文字(省略可)</param>
        [Conditional("DEBUG")]
        public static void Append(string value, string space = SPACE)
        {
            _sb.Append(value);
            _sb.Append(SPACE);
        }

        /// <summary>デバッグ出力バッファに文字を追加します</summary>
        /// <param name="value">値</param>
        /// <param name="space">区切り文字(省略可)</param>
        [Conditional("DEBUG")]
        public static void Append(int value, string space = SPACE)
        {
            _sb.Append(value);
            _sb.Append(SPACE);
        }

        /// <summary>デバッグ出力バッファに文字を追加します</summary>
        /// <param name="value">値</param>
        /// <param name="space">区切り文字(省略可)</param>
        [Conditional("DEBUG")]
        public static void Append(double value, string space = SPACE)
        {
            _sb.Append(value);
            _sb.Append(SPACE);
        }

        /// <summary>デバッグ出力バッファに文字を追加します</summary>
        /// <param name="value">値</param>
        /// <param name="space">区切り文字(省略可)</param>
        [Conditional("DEBUG")]
        public static void Append(float value, string space = SPACE)
        {
            _sb.Append(value);
            _sb.Append(SPACE);
        }

        /// <summary>デバッグ出力バッファに文字を追加します</summary>
        /// <param name="value">値</param>
        /// <param name="space">区切り文字(省略可)</param>
        [Conditional("DEBUG")]
        public static void Append(object value, string space = SPACE)
        {
            _sb.Append(value);
            _sb.Append(SPACE);
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

        /// <summary>バッファをデバッグ出力に出力します</summary>
        [Conditional("DEBUG")]
        internal static void Dump()
        {
            if(_sb.Length == 0) { return; }
            Debug.WriteLine(_sb.ToString());
            _sb.Clear();
        }
    }
}
