using System;
using System.Collections.Generic;
using System.Text;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.Linq;
using Elffy.Input;
using Elffy.UI;
using Elffy.Core;
using System.Drawing;
using System.IO;
using Elffy.Threading;
using System.Diagnostics;
using Elffy.Core.Timer;
using Elffy.Platforms;

namespace Elffy
{
    /// <summary>ゲームを実行するためのクラス</summary>
    public class Game
    {
        /// <summary>ゲーム描画領域オブジェクト</summary>
        private IScreenHost _gameScreen;
        /// <summary>ゲームの実行時間計測用タイマー</summary>
        private readonly IGameTimer _watch = GameTimerGenerator.Create();
        /// <summary>ゲーム起動前に追加されたイベントハンドラーを保持しておくためのバッファ</summary>
        private static readonly List<EventHandler> _temporaryInitializedHandlers = new List<EventHandler>();

        /// <summary><see cref="Game"/> のシングルトンインスタンス</summary>
        public static Game Instance { get; private set; }
        /// <summary>ゲームが起動しているかどうかを取得します</summary>
        public static bool IsRunning => Instance != null;
        public static IUIRoot UIRoot
        {
            get
            {
                ThrowIfGameNotRunning();
                return Instance._gameScreen.UIRoot;
            }
        }

        /// <summary>ゲームの描画領域のピクセルサイズ</summary>
        public static Size ClientSize
        {
            get
            {
                ThrowIfGameNotRunning();
                return Instance._gameScreen.ClientSize;
            }
        }

        public static LayerCollection Layers
        {
            get
            {
                ThrowIfGameNotRunning();
                return Instance._gameScreen.Layers;
            }
        }

        /// <summary>現在のフレームがゲーム開始から何フレーム目かを取得します(Rendering Frame)</summary>
        public static long CurrentFrame { get; internal set; }
        //public static float RenderDelta => (float?)Instance?._window?.RenderPeriod * 1000 ?? throw NewGameNotRunningException();
        public static float RenderDelta => throw new NotImplementedException();     // TODO: 実装
        public static long CurrentFrameTime { get; private set; }
        public static long CurrentTime
        {
            get
            {
                ThrowIfGameNotRunning();
                return Instance._watch.ElapsedMilliseconds;
            }
        }

        #region event Initialized
        /// <summary>ゲーム初期化後に呼ばれるイベント</summary>
        public static event EventHandler Initialized
        {
            add
            {
                if(IsRunning) {
                    Instance._gameScreen.Initialized += value;
                }
                else {
                    _temporaryInitializedHandlers.Add(value);
                }
            }
            remove
            {
                if(IsRunning) {
                    Instance._gameScreen.Initialized -= value;
                }
                else {
                    _temporaryInitializedHandlers.Remove(value);
                }
            }
        }
        #endregion

        private Game(){ }

        #region Run
        /// <summary>ゲームを開始します</summary>
        /// <param name="width">描画領域の幅</param>
        /// <param name="heigh">描画領域の高さ</param>
        /// <param name="title">タイトル</param>
        /// <param name="windowStyle">ウィンドウのスタイル (Windowを用いないプラットフォームでは無効)</param>
        public static void Run(int width, int heigh, string title, WindowStyle windowStyle)
        {
            ThrowIfGameAlreadyRunning();
            try {
                Resources.Initialize();
            }
            catch(Exception) { throw; }
            RunPrivate(width, heigh, title, windowStyle, null);
        }

        /// <summary>ゲームを開始します</summary>
        /// <param name="width">描画領域の幅</param>
        /// <param name="heigh">描画領域の高さ</param>
        /// <param name="title">タイトル</param>
        /// <param name="windowStyle">ウィンドウのスタイル (Windowを用いないプラットフォームでは無効)</param>
        /// <param name="icon">アイコン</param>
        public static void Run(int width, int heigh, string title, WindowStyle windowStyle, string icon)
        {
            ThrowIfGameAlreadyRunning();
            if(string.IsNullOrEmpty(icon)) { throw new ArgumentException($"Icon is null or empty"); }
            try {
                Resources.Initialize();
            }
            catch(Exception) { throw; }
            RunPrivate(width, heigh, title, windowStyle, icon);
        }
        #endregion Run

        #region Exit
        /// <summary>Exit this game.</summary>
        public static void Exit()
        {
            ThrowIfGameNotRunning();
            Instance._gameScreen.Close();
        }
        #endregion

        #region RunPrivate
        /// <summary>ゲームウィンドウを開始します</summary>
        /// <param name="width">ウィンドウ幅</param>
        /// <param name="height">ウィンドウ高さ</param>
        /// <param name="title">ウィンドウタイトル</param>
        /// <param name="windowStyle">ウィンドウスタイル</param>
        /// <param name="iconResourcePath">ウィンドウアイコンのリソースパス(nullならアイコン不使用)</param>
        /// <returns></returns>
        private static void RunPrivate(int width, int height, string title, WindowStyle windowStyle, string iconResourcePath)
        {
            Dispatcher.SetMainThreadID();
            try {
                Instance = new Game();
                switch(Platform.PlatformType) {
                    case PlatformType.Windows:
                    case PlatformType.MacOSX:
                    case PlatformType.Unix:
                        Instance._gameScreen = GetWindowGameScreen(width, height, title, windowStyle, iconResourcePath);
                        break;
                    case PlatformType.Android:
                    case PlatformType.Other:
                    default:
                        throw Platform.NewPlatformNotSupportedException();
                }
                foreach(var handler in _temporaryInitializedHandlers) {
                    Instance._gameScreen.Initialized += handler;
                }
                _temporaryInitializedHandlers.Clear();
                Instance._gameScreen.Run();
            }
            finally {
                Instance?._gameScreen?.Dispose();
                Instance = null;
                ElffySynchronizationContext.Delete();
            }
        }
        #endregion

        #region OnScreenRendering
        /// <summary>フレーム描画前実行処理</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void OnScreenRendering(object sender, EventArgs e)
        {
            CurrentFrameTime = Instance._watch.ElapsedMilliseconds;
            //FPSManager.Aggregate(e.Time);
            if(!Instance._watch.IsRunning) {
                Instance._watch.Start();
            }
        }
        #endregion

        #region OnScreenRendered
        /// <summary>フレーム描画後処理</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void OnScreenRendered(object sender, EventArgs e)
        {
            DebugManager.Next();
            CurrentFrame++;
        }
        #endregion

        #region GetWindowGameScreen
        /// <summary>ウィンドウを用いる OS での <see cref="IScreenHost"/> を取得します</summary>
        /// <param name="width">描画領域の幅</param>
        /// <param name="height">描画領域の高さ</param>
        /// <param name="title">ウィンドウのタイトル</param>
        /// <param name="windowStyle">ウィンドウのスタイル</param>
        /// <param name="iconResourcePath">ウィンドウのアイコンのリソースのパス (nullの場合アイコンなし)</param>
        /// <returns>ウィンドウの <see cref="IScreenHost"/></returns>
        private static IScreenHost GetWindowGameScreen(int width, int height, string title, WindowStyle windowStyle, string iconResourcePath)
        {
            var window = new Window(windowStyle);
            Instance._gameScreen = window;
            if(iconResourcePath != null) {
                if(Resources.HasResource(iconResourcePath)) {
                    using(var stream = Resources.GetStream(iconResourcePath)) {
                        window.Icon = new Icon(stream);
                    }
                }
            }
            window.Title = title;
            window.ClientSize = new Size(width, height);
            window.Rendering += OnScreenRendering;
            window.Rendered += OnScreenRendered;
            return window;
        }
        #endregion

        #region ThrowIfGameNotRunning
        /// <summary>ゲームが起動していない場合に例外を投げます</summary>
        private static void ThrowIfGameNotRunning()
        {
            if(!IsRunning) { throw new InvalidOperationException("Game is Not Running"); }
        }
        #endregion

        #region ThrowIfGameAlreadyRunning
        /// <summary>ゲームが既に起動している間合いに例外を投げます</summary>
        private static void ThrowIfGameAlreadyRunning()
        {
            if(IsRunning) { throw new InvalidOperationException("Game is already Running"); }
        }
        #endregion
    }
}
