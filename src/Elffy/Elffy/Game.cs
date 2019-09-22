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

namespace Elffy
{
    public class Game
    {
        private GameWindow _window;
        private readonly IGameTimer _watch = GameTimerGenerator.Create();
        private readonly RenderingArea _renderingArea = new RenderingArea();

        public static Game Instance { get; private set; }

        public static IUIRoot UIRoot => Instance?._renderingArea?.UIRoot ?? throw NewGameNotRunningException();

        public static Size ClientSize => Instance?._window?.ClientSize ?? throw NewGameNotRunningException();

        public static float AspectRatio
        {
            get
            {
                var clientSize = ClientSize;
                return (float)clientSize.Width / clientSize.Height;
            }
        }

        /// <summary>現在のフレームがゲーム開始から何フレーム目かを取得します(Rendering Frame)</summary>
        public static long CurrentFrame { get; internal set; }
        public static float FrameDelta => (float?)Instance?._window?.UpdatePeriod * 1000 ?? throw NewGameNotRunningException();
        public static float RenderDelta => (float?)Instance?._window?.RenderPeriod * 1000 ?? throw NewGameNotRunningException();
        public static long CurrentFrameTime { get; private set; }
        public static long CurrentTime => Instance?._watch?.ElapsedMilliseconds ?? throw NewGameNotRunningException();

        public static event EventHandler Initialize;

        private Game()
        {
        }

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
        public static void Exit() => Instance?._window?.Close();

        public static bool AddFrameObject(FrameObject frameObject)
        {
            if(Instance == null) { throw NewGameNotRunningException(); }
            return Instance._renderingArea.AddFrameObject(frameObject);
        }

        public static bool RemoveFrameObject(FrameObject frameObject)
        {
            if(Instance == null) { throw NewGameNotRunningException(); }
            return Instance._renderingArea.RemoveFrameObject(frameObject);
        }

        #region FindObject
        public static FrameObject FindObject(string tag)
        {
            if(Instance == null) { throw NewGameNotRunningException(); }
            return Instance._renderingArea.FindObject(tag);
        }
        #endregion

        #region FindAllObject
        public static List<FrameObject> FindAllObject(string tag)
        {
            if(Instance == null) { throw NewGameNotRunningException(); }
            return Instance._renderingArea.FindAllObject(tag);
        }
        #endregion

        #region RunPrivate
        /// <summary>ゲームウィンドウを開始します</summary>
        /// <param name="width">ウィンドウ幅</param>
        /// <param name="heigh">ウィンドウ高さ</param>
        /// <param name="title">ウィンドウタイトル</param>
        /// <param name="windowStyle">ウィンドウスタイル</param>
        /// <param name="iconResourcePath">ウィンドウアイコンのリソースパス(nullならアイコン不使用)</param>
        /// <returns></returns>
        private static void RunPrivate(int width, int heigh, string title, WindowStyle windowStyle, string iconResourcePath)
        {
            Icon icon = null;
            if(iconResourcePath != null) {
                if(Resources.HasResource(iconResourcePath)) {
                    using(var stream = Resources.GetStream(iconResourcePath)) {
                        icon = new Icon(stream);
                    }
                }
            }
            Instance = new Game();
            Instance._renderingArea.Initialized += (sender, e) => { Initialize?.Invoke(Instance, EventArgs.Empty); };
            GameThread.SetMainThreadID();
            try {
                using(var window = new GameWindow(width, heigh, GraphicsMode.Default, title, (GameWindowFlags)windowStyle)) {
                    Instance._window = window;
                    window.ClientSize = new Size(width, heigh);
                    window.Icon = icon;
                    window.Resize += OnResized;
                    window.Load += OnLoaded;
                    window.RenderFrame += OnRendering;
                    window.UpdateFrame += OnFrameUpdating;
                    window.Closed += OnClosed;
                    window.Run();
                    return;
                }
            }
            finally {
                // リソースの解放
                var window = Instance._window;
                window.Load -= OnLoaded;
                window.RenderFrame -= OnRendering;
                window.UpdateFrame -= OnFrameUpdating;
                window.Closed -= OnClosed;
                Instance = null;
            }
        }

        private static void OnResized(object sender, EventArgs e)
        {
            Instance._renderingArea.Size = Instance._window.ClientSize;
        }
        #endregion

        #region OnLoaded
        private static void OnLoaded(object sender, EventArgs e)
        {
            Instance._window.VSync = VSyncMode.On;
            Instance._window.TargetRenderFrequency = DisplayDevice.Default.RefreshRate;
            Instance._renderingArea.Initialize();
        }
        #endregion

        #region OnFrameUpdating
        private static void OnFrameUpdating(object sender, OpenTK.FrameEventArgs e)
        {
            //FPSManager.Aggregate(e.Time);
            //Input.Input.Update();
            //foreach(var frameObject in Instance._frameObjectList.Where(x => !x.IsFrozen)) {
            //    if(frameObject.IsStarted == false) {
            //        frameObject.Start();
            //        frameObject.IsStarted = true;
            //    }
            //    frameObject.Update();
            //}
            //if(Instance._removedFrameObjectBuffer.Count > 0) {
            //    foreach(var item in Instance._removedFrameObjectBuffer) {
            //        Instance._frameObjectList.Remove(item);
            //    }
            //}
            //if(Instance._addedFrameObjectBuffer.Count > 0) {
            //    Instance._frameObjectList.AddRange(Instance._addedFrameObjectBuffer);
            //    Instance._addedFrameObjectBuffer.Clear();
            //}
            //DebugManager.Dump();
        }
        #endregion

        #region OnRendering
        private static void OnRendering(object sender, OpenTK.FrameEventArgs e)
        {
            CurrentFrameTime = Instance._watch.ElapsedMilliseconds;
            FPSManager.Aggregate(e.Time);
            if(!Instance._watch.IsRunning) {
                Instance._watch.Start();
            }
            Instance._renderingArea.RenderFrame();
            Instance._window.SwapBuffers();
            DebugManager.Next();
        }
        #endregion

        #region OnClosed
        private static void OnClosed(object sender, EventArgs e)
        {
            if(Instance == null) { throw NewGameNotRunningException(); }
            Instance._renderingArea.Clear();
        }
        #endregion

        private static Exception NewGameNotRunningException() => new InvalidOperationException("Game is Not Running");
    }
}
