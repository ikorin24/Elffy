#nullable enable
using Elffy.InputSystem;
using Elffy.UI;
using Elffy.Core;
using Elffy.Core.Timer;
using System;
using Elffy.Threading.Tasks;

namespace Elffy
{
    /// <summary>プラットフォームごとの画面を抽象化するためのインターフェース</summary>
    public interface IHostScreen : IDisposable
    {
        /// <summary>マウスを取得します</summary>
        Mouse Mouse { get; }
        /// <summary>カメラを取得します</summary>
        Camera Camera { get; }
        /// <summary>UIのルートオブジェクト</summary>
        Page UIRoot { get; }

        public AsyncBackEndPoint AsyncBack { get; }
        /// <summary>描画領域のサイズ [pixel]</summary>
        Vector2i ClientSize { get; set; }

        /// <summary><see cref="FrameObject"/> を保持するためのレイヤーのリスト</summary>
        LayerCollection Layers { get; }

        TimeSpan Time { get; }

        long FrameNum { get; }

        IDefaultResource DefaultResource { get; }

        internal IGameTimer Watch { get; }

        internal TimeSpan FrameDelta { get; }

        /// <summary>初期化時イベント</summary>
        event ActionEventHandler<IHostScreen> Initialized;
        /// <summary>描画前イベント</summary>
        event ActionEventHandler<IHostScreen> Rendering;
        /// <summary>描画後イベント</summary>
        event ActionEventHandler<IHostScreen> Rendered;

        void Show();

        internal void HandleOnce();
    }
}
