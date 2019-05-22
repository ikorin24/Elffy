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

namespace Elffy
{
    public class Game
    {
        private GameWindow _window;
        private List<FrameObject> _frameObjectList = new List<FrameObject>();
        private List<FrameObject> _addedFrameObjectBuffer = new List<FrameObject>();
        private List<FrameObject> _removedFrameObjectBuffer = new List<FrameObject>();

        public static Game Instance { get; private set; }
        public static Size ClientSize => Instance?._window?.ClientSize ?? throw NewGameNotRunningException();
        public static double FrameDelta => Instance?._window?.UpdatePeriod ?? throw NewGameNotRunningException();
        public static double RenderDelta => Instance?._window?.RenderPeriod ?? throw NewGameNotRunningException();

        public static event EventHandler Initialize;

        private Game()
        {
        }

        #region Run
        public static GameExitResult Run(int width, int heigh, string title, WindowStyle windowStyle)
        {
            if(Instance != null) { throw new InvalidOperationException("Game is already Running"); }
            try {
                Resources.Initialize();
            }
            catch(Exception) { return GameExitResult.FailedInInitializingResource; }
            return RunPrivate(width, heigh, title, windowStyle, null);
        }

        public static GameExitResult Run(int width, int heigh, string title, WindowStyle windowStyle, string icon)
        {
            if(Instance != null) { throw new InvalidOperationException("Game is already Running"); }
            if(string.IsNullOrEmpty(icon)) { throw new ArgumentException($"Icon is null or empty"); }
            try {
                Resources.Initialize();
            }
            catch(Exception) { return GameExitResult.FailedInInitializingResource; }
            return RunPrivate(width, heigh, title, windowStyle, icon);
        }
        #endregion Run
        
        /// <summary>Exit this game.</summary>
        public static void Exit() => Instance?._window?.Close();

        #region AddFrameObject
        public static bool AddFrameObject(FrameObject frameObject)
        {
            if(Instance == null) { return false; }
            if(frameObject == null) { return false; }
            Instance._addedFrameObjectBuffer.Add(frameObject);
            return true;
        }
        #endregion

        #region RemoveFrameObject
        public static bool RemoveFrameObject(FrameObject frameObject)
        {
            if(Instance == null) { return false; }
            if(frameObject == null) { return false; }
            Instance._removedFrameObjectBuffer.Add(frameObject);
            return true;
        }
        #endregion

        #region FindObject
        public static FrameObject FindObject(string tag)
        {
            return Instance?._frameObjectList?.Find(x => x.Tag == tag);
        }
        #endregion

        #region FindAllObject
        public static List<FrameObject> FindAllObject(string tag)
        {
            return Instance?._frameObjectList?.FindAll(x => x.Tag == tag) ?? new List<FrameObject>();
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
        private static GameExitResult RunPrivate(int width, int heigh, string title, WindowStyle windowStyle, string iconResourcePath)
        {
            Icon icon = null;
            if(iconResourcePath != null) {
                if(Resources.HasResource(iconResourcePath)) {
                    using(var stream = Resources.LoadStream(iconResourcePath)) {
                        icon = new Icon(stream);
                    }
                }
            }
            Instance = new Game();
            try {
                using(var window = new GameWindow(width, heigh, GraphicsMode.Default, title, (GameWindowFlags)windowStyle)) {
                    Instance._window = window;
                    window.Icon = icon;
                    window.Load += OnLoaded;
                    window.RenderFrame += OnRendering;
                    window.UpdateFrame += OnFrameUpdating;
                    window.Closed += OnClosed;
                    window.Run();
                    return GameExitResult.SuccessfulCompletion;
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
            // OpenGLの初期設定
            GL.ClearColor(Color4.Black);
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.Texture2D);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.Viewport(Instance._window.ClientRectangle);
            Instance._window.VSync = VSyncMode.Adaptive;
            Instance._window.TargetRenderFrequency = DisplayDevice.Default.RefreshRate;
            Initialize?.Invoke(Instance, EventArgs.Empty);
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
            FPSManager.Aggregate(e.Time);
            Input.Input.Update();
            foreach(var frameObject in Instance._frameObjectList.Where(x => !x.IsFrozen)) {
                if(frameObject.IsStarted == false) {
                    frameObject.Start();
                    frameObject.IsStarted = true;
                }
                frameObject.Update();
            }
            if(Instance._removedFrameObjectBuffer.Count > 0) {
                foreach(var item in Instance._removedFrameObjectBuffer) {
                    Instance._frameObjectList.Remove(item);
                }
            }
            if(Instance._addedFrameObjectBuffer.Count > 0) {
                Instance._frameObjectList.AddRange(Instance._addedFrameObjectBuffer);
                Instance._addedFrameObjectBuffer.Clear();
            }

            GL.MatrixMode(MatrixMode.Projection);
            var projection = Camera.Current.Projection;
            GL.LoadMatrix(ref projection);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            var renderables = Instance._frameObjectList.OfType<Renderable>();

            // Render World Object
            foreach(var frameObject in renderables.Where(x => x.Layer == ObjectLayer.World && x.IsVisible)) {
                frameObject.Render();
            }

            GL.MatrixMode(MatrixMode.Projection);
            var uiProjection = UISetting.Projection;
            GL.LoadMatrix(ref uiProjection);

            // Render UI Object
            foreach(var frameObject in renderables.Where(x => x.Layer == ObjectLayer.UI && x.IsVisible)) {
                frameObject.Render();
            }

            Instance._window.SwapBuffers();
            DebugManager.Next();
        }
        #endregion

        #region OnClosed
        private static void OnClosed(object sender, EventArgs e)
        {
            foreach(var item in Instance._frameObjectList) {
                item.Destroy();
            }
            Instance._frameObjectList.Clear();
            Instance._addedFrameObjectBuffer.Clear();
            Instance._removedFrameObjectBuffer.Clear();
        }
        #endregion

        private static Exception NewGameNotRunningException() => new InvalidOperationException("Game is Not Running");
    }

    public enum GameExitResult
    {
        SuccessfulCompletion,
        FailedInInitializingResource,
    }
}
