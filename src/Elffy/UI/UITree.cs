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
    public sealed class UITree
    {
        const float FAR = 1.01f;
        const float NEAR = -0.01f;
        private readonly Page _uiRoot = new Page();
        private Matrix4 _projection = Matrix4.Identity;
        internal Matrix4 Projection => _projection;

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

        private void CalcProjection()
        {
            _projection = Matrix4.CreateOrthographicOffCenter(0, UIWidth, 0, UIHeight, NEAR, FAR);
        }
    }
}
