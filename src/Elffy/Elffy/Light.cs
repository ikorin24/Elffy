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
        private static readonly List<DirectLight> _lightList = new List<DirectLight>(MaxCount);
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

        public static DirectLight GetLight(int id) => _lightList.Find(x => x.ID == id);

        internal static void AddLight(DirectLight light)
        {
            Debug.Assert(Dispatcher.IsMainThread());
            if(!CanCreateNew) { throw new InvalidOperationException("Can not add more Light."); }
            light.LightName = GetLightNumber();
            _lightList.Add(light);
        }

        internal static void RemoveLight(DirectLight light)
        {
            Debug.Assert(Dispatcher.IsMainThread());
            _lightList.Remove(light);
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

    #region class DirectLight
    public sealed class DirectLight : IDestroyable
    {
        private Vector4 _position;

        public bool IsActivated { get; private set; }

        public bool IsDestroyed { get; private set; }

        internal LightName LightName { get; set; }

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

        public int ID => (int)(LightName - LightName.Light0);

        public DirectLight() : this(new Vector3(-1f, -1f, 0f), Color4.Black, Color4.White, Color4.White) { }

        public DirectLight(Vector3 direction) : this(direction, Color4.Black, Color4.White, Color4.White) { }

        public DirectLight(Vector3 direction, Color4 color) : this(direction, Color4.Black, color, color) { }

        public DirectLight(Vector3 direction, Color4 ambient, Color4 diffuse, Color4 specular)
        {
            Direction = direction;
            Ambient = ambient;
            Diffuse = diffuse;
            Specular = specular;
        }

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

        public void LightUp()
        {
            ThrowIfDestroyed();
            Dispatcher.ThrowIfNotMainThread();
            GL.Enable((EnableCap)LightName);
            GL.Light(LightName, LightParameter.Position, _position);
            GL.Light(LightName, LightParameter.Ambient, Ambient);
            GL.Light(LightName, LightParameter.Diffuse, Diffuse);
            GL.Light(LightName, LightParameter.Specular, Specular);
        }

        public void TurnOff()
        {
            ThrowIfDestroyed();
            Dispatcher.ThrowIfNotMainThread();
            GL.Disable((EnableCap)LightName);
        }

        private void ThrowIfDestroyed()
        {
            if(IsDestroyed) { throw new ObjectDestroyedException(this); }
        }
    }
    #endregion
}
