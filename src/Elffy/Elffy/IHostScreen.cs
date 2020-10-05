#nullable enable
using Elffy.InputSystem;
using Elffy.UI;
using Elffy.Core;
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
        RootPanel UIRoot { get; }

        public AsyncBackEndPoint AsyncBack { get; }
        /// <summary>描画領域のサイズ [pixel]</summary>
        Vector2i ClientSize { get; set; }

        Vector2i Location { get; set; }

        /// <summary><see cref="FrameObject"/> を保持するためのレイヤーのリスト</summary>
        LayerCollection Layers { get; }

        /// <summary>Get time of current frame. (This is NOT real time.)</summary>
        ref readonly TimeSpan Time { get; }

        /// <summary>Get number of current frame.</summary>
        ref readonly long FrameNum { get; }

        IDefaultResource DefaultResource { get; }

        internal TimeSpan FrameDelta { get; }

        /// <summary>初期化時イベント</summary>
        event ActionEventHandler<IHostScreen> Initialized;

        void Show();

        /// <summary>Return current thread is main or not of this <see cref="IHostScreen"/></summary>
        /// <returns>true if main thread, otherwise false</returns>
        bool IsThreadMain();

        /// <summary>Throw exception if current thread is not main of this <see cref="IHostScreen"/></summary>
        void ThrowIfNotMainThread();

        internal void HandleOnce();
    }
}
