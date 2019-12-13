#nullable enable
using System;
using System.Collections.Generic;
using Elffy.UI;
using System.Drawing;
using Elffy.Threading;
using Elffy.Core.Timer;
using Elffy.Platforms;
using Elffy.Exceptions;
using Elffy.InputSystem;

namespace Elffy
{
    /// <summary>ゲームを実行するためのクラス</summary>
    public class Game
    {
        /// <summary>ゲーム描画領域オブジェクト</summary>
        private IScreenHost _gameScreen = null!;
        /// <summary>ゲームの実行時間計測用タイマー</summary>
        private readonly IGameTimer _watch = GameTimerGenerator.Create();
        /// <summary>ゲーム起動前に追加されたイベントハンドラーを保持しておくためのバッファ</summary>
        private static readonly List<ActionEventHandler<IScreenHost>> _temporaryInitializedHandlers = new List<ActionEventHandler<IScreenHost>>();

        /// <summary><see cref="Game"/> のシングルトンインスタンス</summary>
        private static Game Instance
        {
            get
            {
                ThrowIfGameNotRunning();
                return _instance!;
            }
        }
        private static Game? _instance;
        /// <summary>ゲームが起動しているかどうかを取得します</summary>
        public static bool IsRunning => _instance != null;
        /// <summary>UI を構成する <see cref="Control"/> のコレクション</summary>
        public static ControlCollection UI => Instance._gameScreen.UIRoot.Children;

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
                    _instance!._gameScreen.TargetRenderPeriod = value.TotalSeconds;
                }
            }
        }
        private static TimeSpan _frameDelta = TimeSpan.FromSeconds(1d / 60d);
        /// <summary>ゲームが始まってから現在のフレームまでの時間</summary>
        public static TimeSpan Time { get; private set; }
        /// <summary>ゲームが始まってからの実時間</summary>
        public static TimeSpan RealTime => Instance._watch.Elapsed;

        public static Dispatcher Dispatcher => Instance._gameScreen.Dispatcher;

        /// <summary>ゲーム初期化後に呼ばれるイベント</summary>
        public static event ActionEventHandler<IScreenHost> Initialized
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

        private Game(){ }

        /// <summary>ゲームを開始します</summary>
        /// <param name="width">描画領域の幅</param>
        /// <param name="height">描画領域の高さ</param>
        /// <param name="title">タイトル</param>
        /// <param name="windowStyle">ウィンドウのスタイル (Windowを用いないプラットフォームでは無効)</param>
        /// /// <param name="uiYAxisDirection">UIのY軸方向</param>
        public static void Run(int width, int height, string title, WindowStyle windowStyle, YAxisDirection uiYAxisDirection)
        {
            ThrowIfGameAlreadyRunning();
            ArgumentChecker.ThrowOutOfRangeIf(width < 0, nameof(width), width, $"{nameof(width)} is out of range");
            ArgumentChecker.ThrowOutOfRangeIf(height < 0, nameof(height), height, $"{nameof(height)} is out of range");
            try {
                Resources.Initialize();
            }
            catch(Exception) { throw; }
            RunPrivate(width, height, title, windowStyle, uiYAxisDirection, null);
        }

        /// <summary>ゲームを開始します</summary>
        /// <param name="width">描画領域の幅</param>
        /// <param name="height">描画領域の高さ</param>
        /// <param name="title">タイトル</param>
        /// <param name="windowStyle">ウィンドウのスタイル (Windowを用いないプラットフォームでは無効)</param>
        /// <param name="uiYAxisDirection">UIのY軸方向</param>
        /// <param name="icon">アイコン</param>
        public static void Run(int width, int height, string title, WindowStyle windowStyle, YAxisDirection uiYAxisDirection, string icon)
        {
            ThrowIfGameAlreadyRunning();
            ArgumentChecker.ThrowOutOfRangeIf(width < 0, nameof(width), width, $"{nameof(width)} is out of range");
            ArgumentChecker.ThrowOutOfRangeIf(height < 0, nameof(height), height, $"{nameof(height)} is out of range");
            ArgumentChecker.ThrowArgumentIf(string.IsNullOrEmpty(icon), "Icon is null or empty");
            try {
                Resources.Initialize();
            }
            catch(Exception) { throw; }
            RunPrivate(width, height, title, windowStyle, uiYAxisDirection, icon);
        }

        /// <summary>Exit this game.</summary>
        public static void Exit()
        {
            Instance._gameScreen.Close();
        }

        /// <summary>ゲームウィンドウを開始します</summary>
        /// <param name="width">ウィンドウ幅</param>
        /// <param name="height">ウィンドウ高さ</param>
        /// <param name="title">ウィンドウタイトル</param>
        /// <param name="windowStyle">ウィンドウスタイル</param>
        /// <param name="uiYAxisDirection">UI のY軸方向</param>
        /// <param name="iconResourcePath">ウィンドウアイコンのリソースパス(nullならアイコン不使用)</param>
        /// <returns></returns>
        private static void RunPrivate(int width, int height, string title, WindowStyle windowStyle, YAxisDirection uiYAxisDirection, string? iconResourcePath)
        {
            try {
                _instance = new Game();
                var gameScreen = Platform.PlatformType switch
                {
                    PlatformType.Windows => GetWindowGameScreen(width, height, title, windowStyle, uiYAxisDirection, iconResourcePath),
                    PlatformType.MacOSX =>  GetWindowGameScreen(width, height, title, windowStyle, uiYAxisDirection, iconResourcePath),
                    PlatformType.Unix =>    GetWindowGameScreen(width, height, title, windowStyle, uiYAxisDirection, iconResourcePath),
                    _ => throw Platform.PlatformNotSupported()
                };
                gameScreen.TargetRenderPeriod = FrameDelta.TotalSeconds;
                gameScreen.Rendering += OnScreenRendering;
                gameScreen.Rendered += OnScreenRendered;
                foreach(var handler in _temporaryInitializedHandlers) {
                    gameScreen.Initialized += handler;
                }
                _temporaryInitializedHandlers.Clear();
                _instance._gameScreen = gameScreen;
                _instance._gameScreen.Dispatcher.SetMainThreadID();
                _instance._gameScreen.Show();
            }
            finally {
                //_instance = null;
                CustomSynchronizationContext.Delete();
            }
        }

        /// <summary>フレーム描画前実行処理</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void OnScreenRendering(IScreenHost sender)
        {
            if(!Instance._watch.IsRunning) {
                Instance._watch.Start();
            }
        }

        /// <summary>フレーム描画後処理</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void OnScreenRendered(IScreenHost sender)
        {
            Time += FrameDelta;
            FrameNum++;
        }

        /// <summary>ウィンドウを用いる OS での <see cref="IScreenHost"/> を取得します</summary>
        /// <param name="width">描画領域の幅</param>
        /// <param name="height">描画領域の高さ</param>
        /// <param name="title">ウィンドウのタイトル</param>
        /// <param name="windowStyle">ウィンドウのスタイル</param>
        /// <param name="uiYAxisDirection">UIのY軸方向</param>
        /// <param name="iconResourcePath">ウィンドウのアイコンのリソースのパス (nullの場合アイコンなし)</param>
        /// <returns>ウィンドウの <see cref="IScreenHost"/></returns>
        private static IScreenHost GetWindowGameScreen(int width, int height, string title, WindowStyle windowStyle, YAxisDirection uiYAxisDirection, string? iconResourcePath)
        {
            var window = new Window(width, height, title, windowStyle, uiYAxisDirection);
            if(iconResourcePath != null) {
                if(Resources.HasResource(iconResourcePath)) {
                    using(var stream = Resources.GetStream(iconResourcePath)) {
                        window.Icon = new Icon(stream);
                    }
                }
            }
            return window;
        }

        /// <summary>ゲームが起動していない場合に例外を投げます</summary>
        private static void ThrowIfGameNotRunning()
        {
            if(!IsRunning) { throw new InvalidOperationException("Game is Not Running"); }
        }

        /// <summary>ゲームが既に起動している間合いに例外を投げます</summary>
        private static void ThrowIfGameAlreadyRunning()
        {
            if(IsRunning) { throw new InvalidOperationException("Game is already Running"); }
        }
    }
}
