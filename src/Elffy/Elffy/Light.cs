#nullable enable
using Elffy.Effective;
using Elffy.Exceptions;
using Elffy.Threading;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Elffy
{
    public static class Light
    {
        /// <summary>Max count of light</summary>
        public const int MaxCount = 8;
        /// <summary>Light list (Max number of light is 8 in OpenTK.)</summary>
        private static readonly Dictionary<int, ILight> _lightList = new Dictionary<int, ILight>(MaxCount);

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
        public static ILight GetLight(int id)
            => ArgumentChecker.GetDicValue(_lightList, id, new KeyNotFoundException($"the light of ID={id} is not found.".AsInterned()));

        /// <summary>add light to list</summary>
        /// <param name="light">light</param>
        internal static void AddLight(ILight light)
        {
            Debug.Assert(Dispatcher.IsMainThread());
            if(!CanCreateNew) { throw new InvalidOperationException("Can not add more Light."); }
            light.LightName = GetLightNumber();
            _lightList.Add(light.ID, light);
        }

        /// <summary>remove light from list</summary>
        /// <param name="light">light</param>
        internal static void RemoveLight(ILight light)
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
}
