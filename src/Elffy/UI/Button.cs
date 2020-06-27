#nullable enable
using Elffy.Exceptions;
using System;

namespace Elffy.UI
{
    /// <summary>処理を実行可能な UI 要素の Button クラス</summary>
    public class Button : Executable
    {
        private const int DEFAULT_WIDTH = 90;
        private const int DEFAULT_HEIGHT = 30;

        /// <summary>コンストラクタ</summary>
        public Button()
        {
            Width = DEFAULT_WIDTH;
            Height = DEFAULT_HEIGHT;
        }

        /// <summary>コンストラクタ</summary>
        /// <param name="width">ボタンの幅</param>
        /// <param name="height">ボタンの高さ</param>
        public Button(int width, int height)
        {
            if(width <= 0) { throw new ArgumentOutOfRangeException(nameof(width), width, "value is 0 or negative."); }
            if(height <= 0) { throw new ArgumentOutOfRangeException(nameof(height), height, "value is 0 or negative."); }
            Width = width;
            Height = height;
        }
    }
}
