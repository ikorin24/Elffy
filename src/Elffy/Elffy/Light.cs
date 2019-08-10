using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elffy
{
    public static class Light
    {
        private static int MAX_COUNT = 8;
        /// <summary>Light list (Max number of light is 8 in OpenTK.)</summary>
        private static readonly List<DirectLight> _lightList = new List<DirectLight>(MAX_COUNT);

        public static bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if(_isEnabled == value) { return; }
                _isEnabled = value;
                if(_isEnabled) {
                    GL.Enable(EnableCap.Lighting);
                }
                else {
                    GL.Disable(EnableCap.Lighting);
                }
            }
        }
        private static bool _isEnabled;

        /// <summary>The number of light</summary>
        public static int Count => _lightList.Count;

        /// <summary>Get whether this can create a new light</summary>
        public static bool CanCreateNew => _lightList.Count < MAX_COUNT;

        #region CreateDirectLight
        /// <summary>Create a direct light</summary>
        public static int CreateDirectLight() => CreateDirectLight(-Vector3.UnitY, Color4.White, Color4.White, Color4.White);
        
        /// <summary>Create a direct light</summary>
        /// <param name="direction">direction of light</param>
        public static int CreateDirectLight(Vector3 direction) => CreateDirectLight(direction, Color4.White, Color4.White, Color4.White);

        /// <summary>Create a direct light</summary>
        /// <param name="direction">direction of light</param>
        /// <param name="color">color of light</param>
        public static int CreateDirectLight(Vector3 direction, Color4 color) => CreateDirectLight(direction, color, color, color);

        /// <summary>Create a direct light</summary>
        /// <param name="direction">direction of light</param>
        /// <param name="ambient">color of ambient light</param>
        /// <param name="diffuse">color of diffuse light</param>
        /// <param name="specular">color of specular light</param>
        public static int CreateDirectLight(Vector3 direction, Color4 ambient, Color4 diffuse, Color4 specular)
        {
            if(!CanCreateNew) { throw new InvalidOperationException("Can not create more Light."); }
            var light = new DirectLight(direction, ambient, diffuse, specular, GetLightNumber());
            _lightList.Add(light);
            return light.ID;
        }
        #endregion

        #region LightUp
        internal static void LightUp()
        {
            foreach(var light in _lightList) {
                light.LightUp();
            }
        }
        #endregion

        #region TurnOff
        internal static void TurnOff()
        {
            foreach(var light in _lightList) {
                light.TurnOff();
            }
        }
        #endregion

        public static DirectLight GetLight(int id) => _lightList.Find(x => x.ID == id);

        public static void RemoveLight(DirectLight light) => _lightList.Remove(light);

        #region GetLightNumber
        private static LightName GetLightNumber()
        {
            switch(_lightList.Count) {
                case 0: return LightName.Light0;
                case 1: return LightName.Light1;
                case 2: return LightName.Light2;
                case 3: return LightName.Light3;
                case 4: return LightName.Light4;
                case 5: return LightName.Light5;
                case 6: return LightName.Light6;
                case 7: return LightName.Light7;
                default:
                    throw new InvalidOperationException();
            }
        }
        #endregion
    }

    #region class DirectLight
    public class DirectLight
    {
        private LightName _lightNumber;
        private Vector4 _position;

        public Vector3 Direction
        {
            get => -_position.Xyz;
            set
            {
                _position = new Vector4(-value);
            }
        }
        public Color4 Ambient { get; set; }
        public Color4 Diffuse { get; set; }
        public Color4 Specular { get; set; }

        public int ID => (int)(_lightNumber - LightName.Light0);

        internal DirectLight(Vector3 direction, Color4 ambient, Color4 diffuse, Color4 specular, LightName number)
        {
            _lightNumber = number;
            Direction = direction;
            Ambient = ambient;
            Diffuse = diffuse;
            Specular = specular;
        }

        internal void LightUp()
        {
            GL.Enable((EnableCap)_lightNumber);
            GL.Light(_lightNumber, LightParameter.Position, _position);
            GL.Light(_lightNumber, LightParameter.Ambient, Ambient);
            GL.Light(_lightNumber, LightParameter.Diffuse, Diffuse);
            GL.Light(_lightNumber, LightParameter.Specular, Specular);
        }

        internal void TurnOff()
        {
            GL.Disable((EnableCap)_lightNumber);
        }
    }
    #endregion
}
