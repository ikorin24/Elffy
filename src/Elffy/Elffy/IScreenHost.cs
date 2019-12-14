#nullable enable
using Elffy.InputSystem;
using Elffy.UI;
using OpenTK;
using System.Drawing;
using Elffy.Threading;
using Elffy.Core.Timer;
using System;

namespace Elffy
{
    /// <summary>プラットフォームごとの画面を抽象化するためのインターフェース</summary>
    public interface IScreenHost
    {
        /// <summary>マウスを取得します</summary>
        Mouse Mouse { get; }
        /// <summary>カメラを取得します</summary>
        Camera Camera { get; }
        /// <summary>垂直同期モード</summary>
        VSyncMode VSync { get; set; }
        /// <summary>レンダリングの間隔 [sec]</summary>
        double TargetRenderPeriod { get; set; }
        /// <summary>UIのルートオブジェクト</summary>
        Page UIRoot { get; }
        /// <summary>描画領域のサイズ [pixel]</summary>
        Size ClientSize { get; set; }
        /// <summary><see cref="FrameObject"/> を保持するためのレイヤーのリスト</summary>
        LayerCollection Layers { get; }

        Dispatcher Dispatcher { get; }

        TimeSpan Time { get; }

        long FrameNum { get; }

        internal IGameTimer Watch { get; }

        internal TimeSpan FrameDelta { get; }

        /// <summary>初期化時イベント</summary>
        event ActionEventHandler<IScreenHost> Initialized;
        /// <summary>描画前イベント</summary>
        event ActionEventHandler<IScreenHost> Rendering;
        /// <summary>描画後イベント</summary>
        event ActionEventHandler<IScreenHost> Rendered;

        /// <summary><see cref="IScreenHost"/> を起動します</summary>
        void Show();
        /// <summary><see cref="IScreenHost"/> を閉じます</summary>
        void Close();
    }
}
