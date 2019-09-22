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
    public class Game
    {
        private IGameScreen _gameScreen;
        private readonly IGameTimer _watch = GameTimerGenerator.Create();
        private static readonly List<EventHandler> _temporaryHandlers = new List<EventHandler>();

        public static Game Instance { get; private set; }
        public static bool IsRunning => Instance != null;
        public static IUIRoot UIRoot => Instance?._gameScreen?.UIRoot ?? throw NewGameNotRunningException();
        public static Size ClientSize => Instance?._gameScreen?.ClientSize ?? throw NewGameNotRunningException();
        /// <summary>現在のフレームがゲーム開始から何フレーム目かを取得します(Rendering Frame)</summary>
        public static long CurrentFrame { get; internal set; }
        //public static float RenderDelta => (float?)Instance?._window?.RenderPeriod * 1000 ?? throw NewGameNotRunningException();
        public static float RenderDelta => throw new NotImplementedException();     // TODO: 実装
        public static long CurrentFrameTime { get; private set; }
        public static long CurrentTime => Instance?._watch?.ElapsedMilliseconds ?? throw NewGameNotRunningException();

        public static event EventHandler Initialized
        {
            add
            {
                if(IsRunning) {
                    Instance._gameScreen.Initialized += value;
                }
                else {
                    _temporaryHandlers.Add(value);
                }
            }
            remove
            {
                if(IsRunning) {
                    Instance._gameScreen.Initialized -= value;
                }
                else {
                    _temporaryHandlers.Remove(value);
                }
            }
        }


        private Game(){ }

        #region Run
        public static void Run(int width, int heigh, string title, WindowStyle windowStyle)
        {
            if(Instance != null) { throw new InvalidOperationException("Game is already Running"); }
            try {
                Resources.Initialize();
            }
            catch(Exception) { throw; }
            RunPrivate(width, heigh, title, windowStyle, null);
        }

        public static void Run(int width, int heigh, string title, WindowStyle windowStyle, string icon)
        {
            if(Instance != null) { throw new InvalidOperationException("Game is already Running"); }
            if(string.IsNullOrEmpty(icon)) { throw new ArgumentException($"Icon is null or empty"); }
            try {
                Resources.Initialize();
            }
            catch(Exception) { throw; }
            RunPrivate(width, heigh, title, windowStyle, icon);
        }
        #endregion Run

        /// <summary>Exit this game.</summary>
        public static void Exit()
        {
            if(Instance == null) { throw NewGameNotRunningException(); }
            Instance._gameScreen.Close();
        }

        public static bool AddFrameObject(FrameObject frameObject)
        {
            if(Instance == null) { throw NewGameNotRunningException(); }
            return Instance._gameScreen.AddFrameObject(frameObject);
        }

        public static bool RemoveFrameObject(FrameObject frameObject)
        {
            if(Instance == null) { throw NewGameNotRunningException(); }
            return Instance._gameScreen.RemoveFrameObject(frameObject);
        }

        #region FindObject
        public static FrameObject FindObject(string tag)
        {
            if(Instance == null) { throw NewGameNotRunningException(); }
            return Instance._gameScreen.FindObject(tag);
        }
        #endregion

        #region FindAllObject
        public static List<FrameObject> FindAllObject(string tag)
        {
            if(Instance == null) { throw NewGameNotRunningException(); }
            return Instance._gameScreen.FindAllObject(tag);
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
            GameThread.SetMainThreadID();
            try {
                Instance = new Game();
                var platform = Platform.GetPlatformType();
                switch(platform) {
                    case PlatformType.Windows:
                        Instance._gameScreen = GetWindowGameScreen(width, height, title, windowStyle, iconResourcePath);
                        break;
                    case PlatformType.MacOSX:
                    case PlatformType.Unix:
                    case PlatformType.Android:
                    case PlatformType.Other:
                    default:
                        throw new PlatformNotSupportedException($"Game can not run on this platform; Platform Type : '{platform}'");
                }
                foreach(var handler in _temporaryHandlers) {
                    Instance._gameScreen.Initialized += handler;
                }
                _temporaryHandlers.Clear();
                Instance._gameScreen.Run();
            }
            finally {
                Instance?._gameScreen?.Dispose();
                Instance = null;
            }            
        }
        #endregion

        private static void OnScreenRendering(object sender, EventArgs e)
        {
            CurrentFrameTime = Instance._watch.ElapsedMilliseconds;
            //FPSManager.Aggregate(e.Time);
            if(!Instance._watch.IsRunning) {
                Instance._watch.Start();
            }
        }

        private static void OnScreenRendered(object sender, EventArgs e)
        {
            DebugManager.Next();
        }

        #region GetWindowGameScreen
        /// <summary>ウィンドウを用いる OS での <see cref="IGameScreen"/> を取得します</summary>
        /// <param name="width">描画領域の幅</param>
        /// <param name="height">描画領域の高さ</param>
        /// <param name="title">ウィンドウのタイトル</param>
        /// <param name="windowStyle">ウィンドウのスタイル</param>
        /// <param name="iconResourcePath">ウィンドウのアイコンのリソースのパス (nullの場合アイコンなし)</param>
        /// <returns>ウィンドウの <see cref="IGameScreen"/></returns>
        private static IGameScreen GetWindowGameScreen(int width, int height, string title, WindowStyle windowStyle, string iconResourcePath)
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

        private static Exception NewGameNotRunningException() => new InvalidOperationException("Game is Not Running");
    }
}
