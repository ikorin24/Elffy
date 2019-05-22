using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elffy.UI
{
    public static class UISetting
    {
        const float FAR = 1.01f;
        const float NEAR = -0.01f;
        public const float Z_MAX = 1f;
        public const float Z_MIN = 0;

        #region Width
        /// <summary>UI Layer Width</summary>
        public static int Width
        {
            get => _width;
            set
            {
                if(value <= 0) { throw new ArgumentException("value must be 0 ~ ."); }
                _width = value;
                CalcProjection();
            }
        }
        private static int _width;
        #endregion

        #region Height
        /// <summary>UI Layer Height</summary>
        public static int Height
        {
            get => _height;
            set
            {
                if(value <= 0) { throw new ArgumentException("value must be 0 ~ ."); }
                _height = value;
                CalcProjection();
            }
        }
        private static int _height;
        #endregion

        public static Matrix4 Projection { get; private set; } = Matrix4.Identity;

        #region constructor
        static UISetting()
        {
            Width = Game.ClientSize.Width;
            Height = Game.ClientSize.Height;
        }
        #endregion

        private static void CalcProjection()
        {
            Projection = Matrix4.CreateOrthographic(_width, _height, NEAR, FAR);
        }
    }
}
