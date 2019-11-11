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
using OpenTK.Input;
using Mouse = Elffy.Input.Mouse;
using Elffy.Exceptions;

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
        private static Game Instance
        {
            get
            {
                ThrowIfGameNotRunning();
                return _instance;
            }
        }
        private static Game _instance;
        /// <summary>ゲームが起動しているかどうかを取得します</summary>
        public static bool IsRunning => _instance != null;
        /// <summary>UI を構成する <see cref="UIBase"/> のコレクション</summary>
        public static UIBaseCollection UI => Instance._gameScreen.UIRoot.Children;

        /// <summary>ゲームの描画領域のピクセルサイズ</summary>
        public static Size GameScreenSize => Instance._gameScreen.ClientSize;
        /// <summary><see cref="FrameObject"/> を格納するレイヤーを取得します</summary>
        public static LayerCollection Layers => Instance._gameScreen.Layers;

        /// <summary>マウスを取得します</summary>
        public static Mouse Mouse => Instance._gameScreen.Mouse;

        /// <summary>カメラを取得します</summary>
        public static Camera Camera => Instance._gameScreen.Camera;

        /// <summary>現在のフレームがゲーム開始から何フレーム目かを取得します</summary>
        public static long FrameNum { get; internal set; }

        /// <summary>フレームの間隔</summary>
        public static TimeSpan FrameDelta
        {
            get => _frameDelta;
            set
            {
                ArgumentChecker.ThrowOutOfRangeIf(value <= TimeSpan.Zero, nameof(value), value, "value is 0 or negative.");
                _frameDelta = value;
                if(IsRunning) {
                    _instance._gameScreen.TargetRenderPeriod = value.TotalSeconds;
                }
            }
        }
        private static TimeSpan _frameDelta = TimeSpan.FromSeconds(1d / 60d);
        /// <summary>ゲームが始まってから現在のフレームまでの時間</summary>
        public static TimeSpan Time { get; private set; }
        /// <summary>ゲームが始まってからの実時間</summary>
        public static TimeSpan RealTime => Instance._watch.Elapsed;

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
        /// <param name="height">描画領域の高さ</param>
        /// <param name="title">タイトル</param>
        /// <param name="windowStyle">ウィンドウのスタイル (Windowを用いないプラットフォームでは無効)</param>
        public static void Run(int width, int height, string title, WindowStyle windowStyle)
        {
            ThrowIfGameAlreadyRunning();
            ArgumentChecker.ThrowOutOfRangeIf(width < 0, nameof(width), width, $"{nameof(width)} is out of range");
            ArgumentChecker.ThrowOutOfRangeIf(height < 0, nameof(height), height, $"{nameof(height)} is out of range");
            try {
                Resources.Initialize();
            }
            catch(Exception) { throw; }
            RunPrivate(width, height, title, windowStyle, null);
        }

        /// <summary>ゲームを開始します</summary>
        /// <param name="width">描画領域の幅</param>
        /// <param name="height">描画領域の高さ</param>
        /// <param name="title">タイトル</param>
        /// <param name="windowStyle">ウィンドウのスタイル (Windowを用いないプラットフォームでは無効)</param>
        /// <param name="icon">アイコン</param>
        public static void Run(int width, int height, string title, WindowStyle windowStyle, string icon)
        {
            ThrowIfGameAlreadyRunning();
            ArgumentChecker.ThrowOutOfRangeIf(width < 0, nameof(width), width, $"{nameof(width)} is out of range");
            ArgumentChecker.ThrowOutOfRangeIf(height < 0, nameof(height), height, $"{nameof(height)} is out of range");
            ArgumentChecker.ThrowArgumentIf(string.IsNullOrEmpty(icon), "Icon is null or empty");
            try {
                Resources.Initialize();
            }
            catch(Exception) { throw; }
            RunPrivate(width, height, title, windowStyle, icon);
        }
        #endregion Run

        #region Exit
        /// <summary>Exit this game.</summary>
        public static void Exit()
        {
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
                _instance = new Game();
                IScreenHost gameScreen;
                switch(Platform.PlatformType) {
                    case PlatformType.Windows:
                    case PlatformType.MacOSX:
                    case PlatformType.Unix:
                        gameScreen = GetWindowGameScreen(width, height, title, windowStyle, iconResourcePath);
                        break;
                    case PlatformType.Android:
                    case PlatformType.Other:
                    default:
                        throw Platform.NewPlatformNotSupportedException();
                }
                gameScreen.TargetRenderPeriod = FrameDelta.TotalSeconds;
                gameScreen.Rendering += OnScreenRendering;
                gameScreen.Rendered += OnScreenRendered;
                foreach(var handler in _temporaryInitializedHandlers) {
                    gameScreen.Initialized += handler;
                }
                _temporaryInitializedHandlers.Clear();
                _instance._gameScreen = gameScreen;
                _instance._gameScreen.Run();
            }
            finally {
                _instance?._gameScreen?.Dispose();
                _instance = null;
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
            Time += FrameDelta;
            FrameNum++;
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
            if(iconResourcePath != null) {
                if(Resources.HasResource(iconResourcePath)) {
                    using(var stream = Resources.GetStream(iconResourcePath)) {
                        window.Icon = new Icon(stream);
                    }
                }
            }
            window.Title = title;
            window.ClientSize = new Size(width, height);
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
