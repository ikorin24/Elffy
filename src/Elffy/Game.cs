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
        private List<GameObject> _gameObjectList = new List<GameObject>();
        private List<GameObject> _addedGameObjectBuffer = new List<GameObject>();
        private List<GameObject> _removedGameObjectBuffer = new List<GameObject>();

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

        #region AddGameObject
        public static bool AddGameObject(GameObject gameObject)
        {
            if(Instance == null) { return false; }
            if(gameObject == null) { return false; }
            Instance._addedGameObjectBuffer.Add(gameObject);
            return true;
        }
        #endregion

        #region RemoveGameObject
        public static bool RemoveGameObject(GameObject gameObject)
        {
            if(Instance == null) { return false; }
            if(gameObject == null) { return false; }
            Instance._removedGameObjectBuffer.Add(gameObject);
            return true;
        }
        #endregion

        #region FindObject
        public static GameObject FindObject(string tag)
        {
            return Instance?._gameObjectList?.Find(x => x.Tag == tag);
        }
        #endregion

        #region FindAllObject
        public static List<GameObject> FindAllObject(string tag)
        {
            return Instance?._gameObjectList?.FindAll(x => x.Tag == tag) ?? new List<GameObject>();
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
                    window.Run(200);
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
            Instance._window.VSync = VSyncMode.On;
            Initialize?.Invoke(Instance, EventArgs.Empty);
        }
        #endregion

        #region OnFrameUpdating
        private static void OnFrameUpdating(object sender, OpenTK.FrameEventArgs e)
        {
            FPSManager.Aggregate(e.Time);
            Input.Input.Update();
            foreach(var gameObject in Instance._gameObjectList.Where(x => !x.IsFrozen)) {
                if(gameObject.IsStarted == false) {
                    gameObject.Start();
                    gameObject.IsStarted = true;
                }
                gameObject.Update();
            }
            if(Instance._removedGameObjectBuffer.Count > 0) {
                foreach(var item in Instance._removedGameObjectBuffer) {
                    Instance._gameObjectList.Remove(item);
                }
            }
            if(Instance._addedGameObjectBuffer.Count > 0) {
                Instance._gameObjectList.AddRange(Instance._addedGameObjectBuffer);
                Instance._addedGameObjectBuffer.Clear();
            }
            DebugManager.Dump();
        }
        #endregion

        #region OnRendering
        private static void OnRendering(object sender, OpenTK.FrameEventArgs e)
        {
            GL.MatrixMode(MatrixMode.Projection);
            var projection = Camera.Current.Projection;
            //GL.Ortho(-1, 1, -1, 1, 0, 100);
            //GL.Frustum(0, 1, 0, 1, 0, 100);         // TODO:
            //projection = Matrix4.CreatePerspectiveFieldOfView(190f / 180 * (float)Math.PI, ClientSize.Width / ClientSize.Height, 0.1f, 64.0f);
            GL.LoadMatrix(ref projection);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            foreach(var gameObject in Instance._gameObjectList.OfType<Renderable>().Where(x => x.IsVisible)) {
                gameObject.Render();
            }
            Instance._window.SwapBuffers();
        }
        #endregion

        #region OnClosed
        private static void OnClosed(object sender, EventArgs e)
        {
            foreach(var item in Instance._gameObjectList) {
                item.Destroy();
            }
            Instance._gameObjectList.Clear();
            Instance._addedGameObjectBuffer.Clear();
            Instance._removedGameObjectBuffer.Clear();
        }
        #endregion

        private static Exception NewGameNotRunningException()
        {
            return new InvalidOperationException("Game is Not Running");
        }
    }

    public enum GameExitResult
    {
        SuccessfulCompletion,
        FailedInInitializingResource,
    }
}
