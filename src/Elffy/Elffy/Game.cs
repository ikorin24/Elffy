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
        private UITree _uiTree;
        private FrameObjectStore _objectStore;
        private GameWindow _window;
        private readonly IGameTimer _watch = GameTimerGenerator.Create();

        public static Game Instance { get; private set; }
        public static Size ClientSize => Instance?._window?.ClientSize ?? throw NewGameNotRunningException();

        public static float AspectRatio
        {
            get
            {
                var clientSize = ClientSize;
                return (float)clientSize.Width / clientSize.Height;
            }
        }

        public static UITree UI => Instance?._uiTree ?? throw NewGameNotRunningException();
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
            return Instance._objectStore.AddFrameObject(frameObject);
        }

        public static bool RemoveFrameObject(FrameObject frameObject)
        {
            if(Instance == null) { throw NewGameNotRunningException(); }
            return Instance._objectStore.RemoveFrameObject(frameObject);
        }

        #region FindObject
        public static FrameObject FindObject(string tag)
        {
            if(Instance == null) { throw NewGameNotRunningException(); }
            return Instance._objectStore.FindObject(tag);
        }
        #endregion

        #region FindAllObject
        public static List<FrameObject> FindAllObject(string tag)
        {
            if(Instance == null) { throw NewGameNotRunningException(); }
            return Instance._objectStore.FindAllObject(tag);
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
            GameThread.SetMainThreadID();
            try {
                using(var window = new GameWindow(width, heigh, GraphicsMode.Default, title, (GameWindowFlags)windowStyle)) {
                    Instance._window = window;
                    window.ClientSize = new Size(width, heigh);
                    window.Icon = icon;
                    window.Load += OnLoaded;
                    window.RenderFrame += OnRendering;
                    window.UpdateFrame += OnFrameUpdating;
                    window.Closed += OnClosed;
                    Instance._uiTree = new UITree(width, heigh);
                    Instance._objectStore = new FrameObjectStore();
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
        #endregion

        #region OnLoaded
        private static void OnLoaded(object sender, EventArgs e)
        {
            GL.ClearColor(Color4.Gray);
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.Texture2D);
            GL.Enable(EnableCap.Normalize);

            // αブレンディング設定
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            // 裏面削除 反時計回りが表でカリング
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);
            GL.FrontFace(FrontFaceDirection.Ccw);

            GL.Viewport(Instance._window.ClientRectangle);
            Instance._window.VSync = VSyncMode.On;
            Instance._window.TargetRenderFrequency = DisplayDevice.Default.RefreshRate;
            Initialize?.Invoke(Instance, EventArgs.Empty);
            Instance._objectStore.ApplyChanging();              // Initializeの中でのFrameObjectの変更を適用
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
            Input.Input.Update();
            if(!Instance._watch.IsRunning) {
                Instance._watch.Start();
            }
            CurrentFrame = Instance._watch.ElapsedMilliseconds;
            Instance._objectStore.Update();
            GameThread.DoInvokedAction();       // Invokeされた処理を実行
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            Light.LightUp();                                        // 光源点灯
            Instance._objectStore.Render(Camera.Current.Projection, Camera.Current.Matrix);
            Light.TurnOff();                                        // 光源消灯
            Instance._objectStore.RenderUI(Instance._uiTree.Projection);
            Instance._window.SwapBuffers();
            Instance._objectStore.ApplyChanging();
            DebugManager.Next();
            CurrentFrame++;
        }
        #endregion

        #region OnClosed
        private static void OnClosed(object sender, EventArgs e)
        {
            if(Instance == null) { throw NewGameNotRunningException(); }
            Instance._objectStore.Clear();
        }
        #endregion

        private static Exception NewGameNotRunningException() => new InvalidOperationException("Game is Not Running");
    }
}
