﻿using System;
using System.Drawing;

namespace Elffy.UI
{
    #region class Button
    /// <summary>処理を実行可能な UI 要素の Button クラス</summary>
    public class Button : Executable
    {
        private const int DEFAULT_WIDTH = 90;
        private const int DEFAULT_HEIGHT = 30;
        private readonly Canvas _canvas;

        #region Content
        /// <summary>ボタンのコンテンツ</summary>
        public Bitmap Content
        {
            get => _content;
            set
            {
                _content = value;
                _canvas.Clear(Color.Transparent);
                _canvas.DrawImage(_content, new Point());
            }
        }
        private Bitmap _content;
        #endregion

        #region コンストラクタ
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
            if(height <= 0) { throw new ArgumentOutOfRangeException(nameof(height), height, "value is 0 or negative"); }
            Width = width;
            Height = height;
        }
        #endregion
    }
    #endregion
}