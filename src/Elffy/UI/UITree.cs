using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elffy.UI
{
    public class UITree
    {
        const float FAR = 1.01f;
        const float NEAR = -0.01f;
        private List<FrameObject> _frameObjectList = new List<FrameObject>();
        private List<FrameObject> _addedFrameObjectBuffer = new List<FrameObject>();
        private List<FrameObject> _removedFrameObjectBuffer = new List<FrameObject>();
        private readonly Page _uiRoot = new Page();
        private Matrix4 _projection = Matrix4.Identity;

        #region UIWidth
        /// <summary>UI Layer Width</summary>
        public int UIWidth
        {
            get => _uiRoot.Width;
            set
            {
                if(value <= 0) { throw new ArgumentException("value must be 0 ~ ."); }
                _uiRoot.Width = value;
                CalcProjection();
            }
        }
        #endregion

        #region UIHeight
        /// <summary>UI Layer Height</summary>
        public int UIHeight
        {
            get => _uiRoot.Height;
            set
            {
                if(value <= 0) { throw new ArgumentException("value must be 0 ~ ."); }
                _uiRoot.Height = value;
                CalcProjection();
            }
        }
        #endregion

        public UIBaseCollection RootChildren => _uiRoot.Children;

        #region constructor
        internal UITree(int width, int height)
        {
            UIWidth = width;
            UIHeight = height;
        }
        #endregion

        internal void AddFrameObject(FrameObject frameObject)
        {
            _addedFrameObjectBuffer.Add(frameObject);
        }

        internal void RemoveFrameObject(FrameObject frameObject)
        {
            _removedFrameObjectBuffer.Add(frameObject);
        }

        internal void RenderUI()
        {
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadMatrix(ref _projection);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();
            GL.Translate(-UIWidth / 2, -UIHeight / 2, 0);

            // Render UI Object
            if(_uiRoot.Renderer.IsVisible) {
                _uiRoot.Renderer.Render();
            }
            foreach(var renderer in _uiRoot.GetOffspring().Select(c => c.Renderer).Where(r => r.IsVisible)) {
                renderer.Render();
            }
        }

        private void CalcProjection()
        {
            _projection = Matrix4.CreateOrthographic(_uiRoot.Width, _uiRoot.Height, NEAR, FAR);
        }
    }
}
