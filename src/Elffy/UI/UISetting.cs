using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elffy.UI
{
    public class UISetting
    {
        const float FAR = 1.01f;
        const float NEAR = -0.01f;
        public const float Z_MAX = 1f;
        public const float Z_MIN = 0;

        #region Width
        /// <summary>UI Layer Width</summary>
        public int Width
        {
            get => _width;
            set
            {
                if(value <= 0) { throw new ArgumentException("value must be 0 ~ ."); }
                _width = value;
                CalcProjection();
            }
        }
        private int _width;
        #endregion

        #region Height
        /// <summary>UI Layer Height</summary>
        public int Height
        {
            get => _height;
            set
            {
                if(value <= 0) { throw new ArgumentException("value must be 0 ~ ."); }
                _height = value;
                CalcProjection();
            }
        }
        private int _height;
        #endregion

        public Matrix4 Projection { get; private set; } = Matrix4.Identity;

        #region constructor
        internal UISetting(int width, int height)
        {
            Width = width;
            Height = height;
        }
        #endregion

        private void CalcProjection()
        {
            Projection = Matrix4.CreateOrthographic(_width, _height, NEAR, FAR);
        }
    }
}
