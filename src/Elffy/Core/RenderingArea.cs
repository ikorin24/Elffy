using Elffy.Core;
using Elffy.Threading;
using Elffy.UI;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elffy.Core
{
    /// <summary>
    /// OpenGL が描画を行う領域を扱うクラスです。
    /// </summary>
    internal class RenderingArea
    {
        const float UI_FAR = 1.01f;
        const float UI_NEAR = -0.01f;
        private readonly FrameObjectStore _objectStore = new FrameObjectStore();
        private Matrix4 _uiProjection = Matrix4.Identity;

        public IUIRoot UIRoot => _uiRoot;
        private readonly Page _uiRoot = new Page();

        /// <summary>現在のフレームが開始から何フレーム目かを取得します</summary>
        public long CurrentFrame { get; private set; }

        public event EventHandler Initialized;

        #region Width
        public int Width
        {
            get => _width;
            set
            {
                if(value < 0) { throw new ArgumentOutOfRangeException(); }
                _width = value;
                OnSizeChanged(0, 0, _width, _height);
            }
        }
        private int _width;
        #endregion

        #region Height
        public int Height
        {
            get => _height;
            set
            {
                if(value < 0) { throw new ArgumentOutOfRangeException(); }
                _height = value;
                OnSizeChanged(0, 0, _width, _height);
            }
        }
        private int _height;
        #endregion

        #region Size
        public Size Size
        {
            get => new Size(_width, _height);
            set
            {
                if(value.Width < 0 || value.Height < 0) { throw new ArgumentOutOfRangeException(); }
                _width = value.Width;
                _height = value.Height;
                OnSizeChanged(0, 0, _width, _height);
            }
        }
        #endregion

        #region Initialize
        /// <summary>描画に関する初期設定を行います</summary>
        public void Initialize()
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

            //_uiRoot.Renderable.Activate();
            Initialized?.Invoke(this, EventArgs.Empty);
            _objectStore.ApplyChanging();
        }
        #endregion

        #region RenderFrame
        /// <summary>フレームを更新して描画します</summary>
        public void RenderFrame()
        {
            Input.Input.Update();
            _objectStore.Update();
            GameThread.DoInvokedAction();                           // Invokeされた処理を実行
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            Light.LightUp();                                        // 光源点灯
            _objectStore.Render(Camera.Current.Projection, Camera.Current.Matrix);
            Light.TurnOff();                                        // 光源消灯
            _objectStore.RenderUI(_uiProjection);
            _objectStore.ApplyChanging();
            CurrentFrame++;
        }
        #endregion

        public bool AddFrameObject(FrameObject frameObject) => _objectStore.AddFrameObject(frameObject);

        public bool RemoveFrameObject(FrameObject frameObject) => _objectStore.RemoveFrameObject(frameObject);

        public FrameObject FindObject(string tag) => _objectStore.FindObject(tag);

        public List<FrameObject> FindAllObject(string tag) => _objectStore.FindAllObject(tag);

        public void Clear() => _objectStore.Clear();

        private void OnSizeChanged(int x, int y, int width, int height)
        {
            GL.Viewport(x, y, width, height);
            _uiProjection = Matrix4.CreateOrthographicOffCenter(x, x + width, y, y + height, UI_NEAR, UI_FAR);
        }
    }
}
