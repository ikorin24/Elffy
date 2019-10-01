using Elffy.UI;
using OpenTK;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elffy
{
    /// <summary>プラットフォームごとの画面を抽象化するためのインターフェース</summary>
    internal interface IScreenHost : IDisposable
    {
        /// <summary>垂直同期モード</summary>
        VSyncMode VSync { get; set; }
        /// <summary>レンダリングのリフレッシュレート [Hz]</summary>
        double TargetRenderFrequency { get; set; }
        /// <summary>UIのルートオブジェクト</summary>
        IUIRoot UIRoot { get; }
        /// <summary>描画領域のサイズ [pixel]</summary>
        Size ClientSize { get; }
        /// <summary><see cref="FrameObject"/> を保持するためのレイヤーのリスト</summary>
        LayerCollection Layers { get; }

        /// <summary>初期化時イベント</summary>
        event EventHandler Initialized;
        /// <summary>描画前イベント</summary>
        event EventHandler Rendering;
        /// <summary>描画後イベント</summary>
        event EventHandler Rendered;

        /// <summary><see cref="IScreenHost"/> を起動します</summary>
        void Run();
        /// <summary><see cref="IScreenHost"/> を閉じます</summary>
        void Close();
    }
}
