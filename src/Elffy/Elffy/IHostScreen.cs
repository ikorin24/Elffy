#nullable enable
using Elffy.InputSystem;
using Elffy.UI;
using OpenToolkit;
using System.Drawing;
using Elffy.Threading;
using Elffy.Core.Timer;
using System;
using OpenToolkit.Windowing.Common;
using OpenToolkit.Mathematics;

namespace Elffy
{
    /// <summary>プラットフォームごとの画面を抽象化するためのインターフェース</summary>
    public interface IHostScreen
    {
        /// <summary>マウスを取得します</summary>
        Mouse Mouse { get; }
        /// <summary>カメラを取得します</summary>
        Camera Camera { get; }
        /// <summary>垂直同期モード</summary>
        VSyncMode VSync { get; set; }
        /// <summary>UIのルートオブジェクト</summary>
        Page UIRoot { get; }
        /// <summary>描画領域のサイズ [pixel]</summary>
        //Size ClientSize { get; set; }
        Vector2i ClientSize { get; set; }

        /// <summary><see cref="FrameObject"/> を保持するためのレイヤーのリスト</summary>
        LayerCollection Layers { get; }

        Dispatcher Dispatcher { get; }

        TimeSpan Time { get; }

        long FrameNum { get; }

        internal IGameTimer Watch { get; }

        internal TimeSpan FrameDelta { get; }

        /// <summary>初期化時イベント</summary>
        event ActionEventHandler<IHostScreen> Initialized;
        /// <summary>描画前イベント</summary>
        event ActionEventHandler<IHostScreen> Rendering;
        /// <summary>描画後イベント</summary>
        event ActionEventHandler<IHostScreen> Rendered;

        /// <summary><see cref="IHostScreen"/> を起動します</summary>
        /// <param name="width">width of <see cref="IHostScreen"/></param>
        /// <param name="height">height of <see cref="IHostScreen"/></param>
        /// <param name="title">title of <see cref="IHostScreen"/></param>
        /// <param name="icon">icon of <see cref="IHostScreen"/> (null if no icon.)</param>
        /// <param name="windowStyle">window style of <see cref="IHostScreen"/>. (Only if the platform uses window.)</param>
        internal void Show(int width, int height, string title, Icon? icon, WindowStyle windowStyle);
        /// <summary><see cref="IHostScreen"/> を閉じます</summary>
        internal void Close();
    }
}
