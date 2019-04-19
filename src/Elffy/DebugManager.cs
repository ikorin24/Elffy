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
