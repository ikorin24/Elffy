using Elffy.Exceptions;
using Elffy.Threading;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Elffy.Core;
using Elffy.Effective;
using System.Runtime.InteropServices;

namespace Elffy
{
    public static class Light
    {
        /// <summary>Max count of light</summary>
        public const int MaxCount = 8;
        /// <summary>Light list (Max number of light is 8 in OpenTK.)</summary>
        private static readonly Dictionary<int, DirectLight> _lightList = new Dictionary<int, DirectLight>(MaxCount);

        private static bool _globalAmbientChanged;

        /// <summary>Get whether this can create a new light</summary>
        private static bool CanCreateNew => _lightList.Count < MaxCount;

        internal static bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if(_isEnabled == value) { return; }
                _isEnabled = value;
                if(_isEnabled) {
                    GL.Enable(EnableCap.Lighting);
                    if(_globalAmbientChanged) {
                        SendGlobalAmbient(_globalAmbient);
                        _globalAmbientChanged = false;
                    }
                }
                else {
                    GL.Disable(EnableCap.Lighting);
                }
            }
        }
        private static bool _isEnabled;

        /// <summary>光源から独立した環境光を設定または取得します</summary>
        public static Color4 GlobalAmbient
        {
            get
            {
                Dispatcher.ThrowIfNotMainThread();
                return _globalAmbient;
            }
            set
            {
                Dispatcher.ThrowIfNotMainThread();
                if(_globalAmbient != value) {
                    value.R = (value.R < 0f) ? 0f : value.R;
                    value.G = (value.G < 0f) ? 0f : value.G;
                    value.B = (value.B < 0f) ? 0f : value.B;
                    value.A = (value.A < 0f) ? 0f : value.A;
                    _globalAmbient = value;
                    _globalAmbientChanged = true;
                }
            }
        }
        private static Color4 _globalAmbient = new Color4(0.2f, 0.2f, 0.2f, 1f);    // default value of OpenGL

        /// <summary>The number of light</summary>
        public static int Count => _lightList.Count;

        /// <summary>get light of specified id</summary>
        /// <param name="id">id of the light</param>
        /// <returns>light</returns>
        public static DirectLight GetLight(int id) 
            => ArgumentChecker.GetDicValue(_lightList, id, new KeyNotFoundException($"the light of ID={id} is not found."));

        /// <summary>add light to list</summary>
        /// <param name="light">light</param>
        internal static void AddLight(DirectLight light)
        {
            Debug.Assert(Dispatcher.IsMainThread());
            if(!CanCreateNew) { throw new InvalidOperationException("Can not add more Light."); }
            light.LightName = GetLightNumber();
            _lightList.Add(light.ID, light);
        }

        /// <summary>remove light from list</summary>
        /// <param name="light">light</param>
        internal static void RemoveLight(DirectLight light)
        {
            Debug.Assert(Dispatcher.IsMainThread());
            _lightList.Remove(light.ID);
        }

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

        /// <summary>Send global ambient value to OpenGL</summary>
        /// <param name="color">global ambient value</param>
        private static void SendGlobalAmbient(Color4 color)
        {
            unsafe {
                var ptr = IntPtr.Zero;
                try {
                    ptr = Marshal.AllocHGlobal(sizeof(float) * 4);
                    var array = (float*)ptr;
                    array[0] = color.R;
                    array[1] = color.G;
                    array[2] = color.B;
                    array[3] = color.A;
                    GL.LightModel(LightModelParameter.LightModelAmbient, array);
                }
                finally {
                    if(ptr != IntPtr.Zero) {
                        Marshal.FreeHGlobal(ptr);
                    }
                }
            }
        }
    }

    /// <summary>Direct light class</summary>
    public sealed class DirectLight : IDestroyable
    {
        /// <summary>
        /// The position of this light.<para/>
        /// [NOTE] <para/>
        /// The position of a light is four dimentional like (x, y, z, w). <para/>
        /// Actual position is three dimentional, that is (x/w, y/w, z/w). <para/>
        /// w == 0 means the infinite point directed by (x, y, z). That is Direct light. <para/>
        /// </summary>
        private Vector4 _position;

        /// <summary>whether this light is lit up</summary>
        private bool _isLitUp;

        /// <summary>get whether this light is activated</summary>
        public bool IsActivated { get; private set; }

        /// <summary>get whether this light is destroyed</summary>
        public bool IsDestroyed { get; private set; }

        /// <summary>light name</summary>
        internal LightName LightName { get; set; }

        /// <summary>get or set direction of this light</summary>
        public Vector3 Direction
        {
            get => -_position.Xyz;
            set
            {
                _position = new Vector4(-value);
            }
        }
        /// <summary>get or set ambient value of this light</summary>
        public Color4 Ambient { get; set; }
        /// <summary>get or set diffuse value of this light</summary>
        public Color4 Diffuse { get; set; }
        /// <summary>get or set specular value of this light</summary>
        public Color4 Specular { get; set; }
        /// <summary>get ID of this light</summary>
        public int ID => (int)(LightName - LightName.Light0);

        /// <summary>
        /// Create <see cref="DirectLight"/> instance.<para/>
        /// Direction = (-1,-1, 0) --- (X, Y, Z)<para/>
        /// [Ambient, Diffuse, Specular] = [(0, 0, 0, 1), (1, 1, 1, 1), (1, 1, 1, 1)] --- (R, G, B, A)<para/>
        /// </summary>
        public DirectLight() : this(new Vector3(-1f, -1f, 0f), Color4.Black, Color4.White, Color4.White) { }

        /// <summary>
        /// Create <see cref="DirectLight"/> instance of specified direction.<para/>
        /// [Ambient, Diffuse, Specular] = [(0, 0, 0, 1), (1, 1, 1, 1), (1, 1, 1, 1)] --- (R, G, B, A)<para/>
        /// </summary>
        /// <param name="direction">direction of the light</param>
        public DirectLight(Vector3 direction) : this(direction, Color4.Black, Color4.White, Color4.White) { }

        /// <summary>
        /// Create <see cref="DirectLight"/> instance of specified direction and specified diffuse &amp; specular.<para/>
        /// Ambient = (0, 0, 0, 1) --- (R, G, B, A)<para/>
        /// </summary>
        /// <param name="direction">direction of the light</param>
        /// <param name="color">diffuse and specular value</param>
        public DirectLight(Vector3 direction, Color4 color) : this(direction, Color4.Black, color, color) { }

        /// <summary>Create <see cref="DirectLight"/> instance of specified direction and specified ambient, diffuse, specular.</summary>
        /// <param name="direction">direction of the light</param>
        /// <param name="ambient">ambient value</param>
        /// <param name="diffuse">diffuse value</param>
        /// <param name="specular">specular value</param>
        public DirectLight(Vector3 direction, Color4 ambient, Color4 diffuse, Color4 specular)
        {
            Direction = direction;
            Ambient = ambient;
            Diffuse = diffuse;
            Specular = specular;
        }

        /// <summary>Activate this light</summary>
        public void Activate()
        {
            ThrowIfDestroyed();
            if(IsActivated) { return; }
            Dispatcher.Invoke(() =>
            {
                IsActivated = true;
                Light.AddLight(this);
                LightUp();
            });
        }

        /// <summary>Destroy this light</summary>
        public void Destroy()
        {
            ThrowIfDestroyed();
            Dispatcher.Invoke(() =>
            {
                IsDestroyed = true;
                Light.RemoveLight(this);
                TurnOff();
            });
        }

        /// <summary>Light up this light</summary>
        public void LightUp()
        {
            ThrowIfDestroyed();
            Dispatcher.ThrowIfNotMainThread();
            if(_isLitUp == false) {
                GL.Enable((EnableCap)LightName);
                GL.Light(LightName, LightParameter.Position, _position);
                GL.Light(LightName, LightParameter.Ambient, Ambient);
                GL.Light(LightName, LightParameter.Diffuse, Diffuse);
                GL.Light(LightName, LightParameter.Specular, Specular);
                _isLitUp = true;
            }
        }

        /// <summary>Turn off this light</summary>
        public void TurnOff()
        {
            ThrowIfDestroyed();
            Dispatcher.ThrowIfNotMainThread();
            if(_isLitUp) {
                GL.Disable((EnableCap)LightName);
                _isLitUp = false;
            }
        }

        /// <summary>Throw exception if the instance is destroyed.</summary>
        private void ThrowIfDestroyed()
        {
            if(IsDestroyed) { throw new ObjectDestroyedException(this); }
        }
    }
}
