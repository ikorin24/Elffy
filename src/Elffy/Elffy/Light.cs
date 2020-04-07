#nullable enable
using Elffy.Effective;
using Elffy.Exceptions;
using Elffy.Threading;
using OpenTK.Graphics.OpenGL;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Elffy
{
    public class Light
    {
        /// <summary>Max count of light</summary>
        public const int MaxCount = 8;

        private bool _globalAmbientChanged;
        /// <summary>Light list (Max number of light is 8 in OpenTK.)</summary>
        private readonly ILight?[] _lightList = new ILight?[MaxCount];
        /// <summary>Get whether this can create a new light</summary>
        private bool CanCreateNew => Count < MaxCount;

        /// <summary>The number of light</summary>
        public int Count { get; private set; }

        internal unsafe bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if(_isEnabled == value) { return; }
                _isEnabled = value;
                if(_isEnabled) {
                    GL.Enable(EnableCap.Lighting);
                    if(_globalAmbientChanged) {
                        var globalAmbient = _globalAmbient;
                        GL.LightModel(LightModelParameter.LightModelAmbient, (float*)&globalAmbient);
                        _globalAmbientChanged = false;
                    }
                }
                else {
                    GL.Disable(EnableCap.Lighting);
                }
            }
        }
        private bool _isEnabled;

        /// <summary>光源から独立した環境光を設定または取得します</summary>
        public Color4 GlobalAmbient
        {
            get
            {
                //CurrentScreen.Dispatcher.ThrowIfNotMainThread();
                return _globalAmbient;
            }
            set
            {
                //CurrentScreen.Dispatcher.ThrowIfNotMainThread();
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
        private Color4 _globalAmbient = new Color4(0.2f, 0.2f, 0.2f, 1f);    // default value of OpenGL

#if DEBUG
        static Light()
        {
            var light0 = LightName.Light0;
            const string message = "Invalid enum value. This may be because implementation of OpenTK changes.";
            Debug.Assert(LightName.Light0 - 0 == light0, message);
            Debug.Assert(LightName.Light1 - 1 == light0, message);
            Debug.Assert(LightName.Light2 - 2 == light0, message);
            Debug.Assert(LightName.Light3 - 3 == light0, message);
            Debug.Assert(LightName.Light4 - 4 == light0, message);
            Debug.Assert(LightName.Light5 - 5 == light0, message);
            Debug.Assert(LightName.Light6 - 6 == light0, message);
            Debug.Assert(LightName.Light7 - 7 == light0, message);
        }
#endif

        internal Light() { }

        /// <summary>add light to list</summary>
        /// <param name="light">light</param>
        internal void AddLight(ILight light)
        {
            Debug.Assert(CurrentScreen.Dispatcher.IsMainThread());
            if(!CanCreateNew) { throw new InvalidOperationException("Can not add more Light."); }
            light.LightName = Count + LightName.Light0;
            _lightList[Count] = light;
            Count++;
        }

        /// <summary>remove light from list</summary>
        /// <param name="light">light</param>
        internal void RemoveLight(ILight light)
        {
            Debug.Assert(CurrentScreen.Dispatcher.IsMainThread());
            _lightList[light.LightName - LightName.Light0] = null;
            Count--;
        }
    }

    /// <summary>
    /// Implementation of <see cref="ILight"/>
    /// </summary>
    /// <remarks>
    /// This class is minimum implementation of <see cref="ILight"/>. There are no argument checking and object state checking.
    /// </remarks>
    internal class LightImpl
    {
        private Light? _manager;

        /// <summary>
        /// The position of this light.<para/>
        /// [NOTE] <para/>
        /// The position of a light is four dimentional like (x, y, z, w). <para/>
        /// Actual position is three dimentional, that is (x/w, y/w, z/w). <para/>
        /// w == 0 means the infinite point directed by (x, y, z). That is Direct light. <para/>
        /// </summary>
        internal Vector4 Position { get; set; }

        /// <summary>get or set whether the light is activated</summary>
        internal bool IsActivated { get; set; }

        internal bool IsTerminated { get; set; }

        /// <summary>get or set ambient value of this light</summary>
        internal Color4 Ambient { get; set; }
        /// <summary>get or set diffuse value of this light</summary>
        internal Color4 Diffuse { get; set; }
        /// <summary>get or set specular value of this light</summary>
        internal Color4 Specular { get; set; }

        internal LightName LightName { get; set; }

        /// <summary>whether this light is lit up</summary>
        internal bool IsLitUp { get; set; }

        internal void LightUp()
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

        internal void TurnOff()
        {
            if(IsLitUp) {
                GL.Disable((EnableCap)LightName);
                IsLitUp = false;
            }
        }

        internal void Activate(ILight light, Light manager)
        {
            IsActivated = true;
            _manager = manager;
            manager.AddLight(light);
        }

        internal void Terminate(ILight light)
        {
            IsTerminated = true;
            _manager!.RemoveLight(light);
        }
    }
}
