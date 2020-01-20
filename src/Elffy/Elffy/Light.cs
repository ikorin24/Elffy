#nullable enable
using Elffy.Exceptions;
using Elffy.Threading;
using Elffy.Effective;
using OpenTK;
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
                Engine.CurrentScreen.Dispatcher.ThrowIfNotMainThread();
                return _globalAmbient;
            }
            set
            {
                Engine.CurrentScreen.Dispatcher.ThrowIfNotMainThread();
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
            => _lightList.GetValueWithKeyChecking(id, "the light of specified ID is not found.");

        /// <summary>add light to list</summary>
        /// <param name="light">light</param>
        private static void AddLight(ILight light)
        {
            Debug.Assert(Engine.CurrentScreen.Dispatcher.IsMainThread());
            if(!CanCreateNew) { throw new InvalidOperationException("Can not add more Light."); }
            light.LightName = GetLightNumber();
            _lightList.Add(light.ID, light);
        }

        /// <summary>remove light from list</summary>
        /// <param name="light">light</param>
        private static void RemoveLight(ILight light)
        {
            Debug.Assert(Engine.CurrentScreen.Dispatcher.IsMainThread());
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
        private static unsafe void SendGlobalAmbient(Color4 color)
        {
            GL.LightModel(LightModelParameter.LightModelAmbient, color.AsSpan<Color4, float>().AsPointer());
        }


        /// <summary>
        /// Implementation of <see cref="ILight"/>
        /// </summary>
        /// <remarks>
        /// This class is minimum implementation of <see cref="ILight"/>. There are no argument checking and object state checking.
        /// </remarks>
        internal class LightImpl
        {
            /// <summary>
            /// The position of this light.<para/>
            /// [NOTE] <para/>
            /// The position of a light is four dimentional like (x, y, z, w). <para/>
            /// Actual position is three dimentional, that is (x/w, y/w, z/w). <para/>
            /// w == 0 means the infinite point directed by (x, y, z). That is Direct light. <para/>
            /// </summary>
            public Vector4 Position { get; set; }

            /// <summary>get or set whether the light is activated</summary>
            public bool IsActivated { get; set; }

            /// <summary>get or set ambient value of this light</summary>
            public Color4 Ambient { get; set; }
            /// <summary>get or set diffuse value of this light</summary>
            public Color4 Diffuse { get; set; }
            /// <summary>get or set specular value of this light</summary>
            public Color4 Specular { get; set; }

            public LightName LightName { get; set; }

            /// <summary>whether this light is lit up</summary>
            public bool IsLitUp { get; set; }

            /// <summary>get whether this light is destroyed</summary>
            public bool IsDestroyed { get; set; }

            public void LightUp()
            {
                if(IsLitUp == false) {
                    GL.Enable((EnableCap)LightName);
                    GL.Light(LightName, LightParameter.Position, Position);
                    GL.Light(LightName, LightParameter.Ambient, Ambient);
                    GL.Light(LightName, LightParameter.Diffuse, Diffuse);
                    GL.Light(LightName, LightParameter.Specular, Specular);
                    IsLitUp = true;
                }
            }

            public void TurnOff()
            {
                if(IsLitUp) {
                    GL.Disable((EnableCap)LightName);
                    IsLitUp = false;
                }
            }

            public void Activate(ILight light)
            {
                IsActivated = true;
                Light.AddLight(light);
                LightUp();
            }

            public void Terminate(ILight light)
            {
                IsDestroyed = true;
                Light.RemoveLight(light);
                TurnOff();
            }
        }
    }
}
