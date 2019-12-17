#nullable enable
#region License
//
// The Open Toolkit Library License
//
// Copyright (c) 2006 - 2009 the Open Toolkit library, except where noted.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights to 
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
// the Software, and to permit persons to whom the Software is furnished to do
// so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
//
#endregion

using System;
using System.ComponentModel;
using System.Windows.Forms;
using OpenTK.Platform;
using OpenTK.Graphics;
using System.Security;
using System.Runtime.InteropServices;
using System.Drawing;
using OpenTK;

namespace Elffy.Platforms.Windows
{
    /// <summary>
    /// OpenGL-aware WinForms control.
    /// The WinForms designer will always call the default constructor.
    /// Inherit from this class and call one of its specialized constructors
    /// to enable antialiasing or custom <see cref="GraphicsMode"/>s.
    /// </summary>
    public partial class GLControl : UserControl
    {
        private IGraphicsContext? _context;
        private IGLControlImpl? _implementation;
        private readonly GraphicsMode _format;
        private readonly int _major;
        private readonly int _minor;
        private readonly GraphicsContextFlags _flags;
        private bool? _initialVsyncValue;
        // Indicates that OnResize was called before OnHandleCreated.
        // To avoid issues with missing OpenGL contexts, we suppress
        // the premature Resize event and raise it as soon as the handle
        // is ready.
        private bool _resizeEventSuppressed;
        // Indicates whether the control is in design mode. Due to issues
        // wiith the DesignMode property and nested controls,we need to
        // evaluate this in the constructor.
        private readonly bool _isDesignMode;
        private IGLControlImpl Implementation { get { ValidateState(); return _implementation!; } }
        private IGraphicsContext Context { get { ValidateState(); return _context!; } }

        protected bool IsDesignMode => _isDesignMode;

        /// <summary>
        /// Gets a value indicating whether the current thread contains pending system messages.
        /// </summary>
        [Browsable(false)]
        public bool IsIdle => Implementation.IsIdle;

        /// <summary>
        /// Gets the GraphicsMode of the GraphicsContext attached to this GLControl.
        /// </summary>
        /// <remarks>
        /// To change the GraphicsMode, you must destroy and recreate the GLControl.
        /// </remarks>
        public GraphicsMode GraphicsMode => Context.GraphicsMode;

        /// <summary>
        /// Gets the <see cref="OpenTK.Platform.IWindowInfo"/> for this instance.
        /// </summary>
        public IWindowInfo WindowInfo => Implementation.WindowInfo;

        public VSyncMode VSync
        {
            get
            {
                var swapInterval = Context.SwapInterval;
                return swapInterval < 0 ? VSyncMode.Adaptive :
                       swapInterval == 0 ? VSyncMode.Off :
                                           VSyncMode.On;
            }
            set
            {
                Context.SwapInterval = value switch
                {
                    VSyncMode.On => 1,
                    VSyncMode.Off => 0,
                    VSyncMode.Adaptive => -1,
                    _ => throw new ArgumentException(),
                };
            }
        }

        /// <summary>
        /// Constructs a new instance.
        /// </summary>
        public GLControl() : this(GraphicsMode.Default) { }

        /// <summary>
        /// Constructs a new instance with the specified GraphicsMode.
        /// </summary>
        /// <param name="mode">The OpenTK.Graphics.GraphicsMode of the control.</param>
        public GLControl(GraphicsMode mode) : this(mode, 1, 0, GraphicsContextFlags.Default) { }

        /// <summary>
        /// Constructs a new instance with the specified GraphicsMode.
        /// </summary>
        /// <param name="mode">The OpenTK.Graphics.GraphicsMode of the control.</param>
        /// <param name="major">The major version for the OpenGL GraphicsContext.</param>
        /// <param name="minor">The minor version for the OpenGL GraphicsContext.</param>
        /// <param name="flags">The GraphicsContextFlags for the OpenGL GraphicsContext.</param>
        public GLControl(GraphicsMode mode, int major, int minor, GraphicsContextFlags flags)
        {
            if(Platform.PlatformType != PlatformType.Windows) { throw Platform.PlatformNotSupported(); }

            _format = mode ?? throw new ArgumentNullException(nameof(mode));
            _major = major;
            _minor = minor;
            _flags = flags;
            SetStyle(ControlStyles.Opaque, true);
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            DoubleBuffered = false;
            _initialVsyncValue = true;

            // Note: the DesignMode property may be incorrect when nesting controls.
            // We use LicenseManager.UsageMode as a workaround (this only works in
            // the constructor).
            _isDesignMode = DesignMode || (LicenseManager.UsageMode == LicenseUsageMode.Designtime);

            InitializeComponent();
        }

        /// <summary>
        /// Swaps the front and back buffers, presenting the rendered scene to the screen.
        /// </summary>
        public void SwapBuffers()
        {
            ValidateState();
            Context.SwapBuffers();
        }

        /// <summary>
        /// Makes the underlying this GLControl current in the calling thread.
        /// All OpenGL commands issued are hereafter interpreted by this GLControl.
        /// </summary>
        public void MakeCurrent() => Context.MakeCurrent(Implementation.WindowInfo);

        /// <summary>Raises the HandleCreated event.</summary>
        /// <param name="e">Not used.</param>
        protected override void OnHandleCreated(EventArgs e)
        {
            _context?.Dispose();
            _implementation?.WindowInfo.Dispose();

            _implementation = _isDesignMode ? new DummyGLControlImpl() as IGLControlImpl : 
                                              new WinGLControlImpl(_format, Handle) as IGLControlImpl;

            _context = _implementation.CreateContext(_major, _minor, _flags);
            MakeCurrent();

            if(!_isDesignMode) {
                ((IGraphicsContextInternal)Context).LoadAll();
            }

            // Deferred setting of vsync mode. See VSync property for more information.
            if(_initialVsyncValue.HasValue) {
                Context.SwapInterval = _initialVsyncValue.Value ? 1 : 0;
                _initialVsyncValue = null;
            }

            base.OnHandleCreated(e);

            if(_resizeEventSuppressed) {
                OnResize(EventArgs.Empty);
                _resizeEventSuppressed = false;
            }
        }

        /// <summary>Raises the HandleDestroyed event.</summary>
        /// <param name="e">Not used.</param>
        protected override void OnHandleDestroyed(EventArgs e)
        {
            if(_context != null) {
                _context.Dispose();
                _context = null;
            }
            if(_implementation != null) {
                _implementation.WindowInfo.Dispose();
                _implementation = null;
            }
            base.OnHandleDestroyed(e);
        }

        /// <summary>
        /// Raises the System.Windows.Forms.Control.Paint event.
        /// </summary>
        /// <param name="e">A System.Windows.Forms.PaintEventArgs that contains the event data.</param>
        protected override void OnPaint(PaintEventArgs e)
        {
            ValidateState();
            if(_isDesignMode) {
                e.Graphics.Clear(Color.Gray);
            }
            base.OnPaint(e);
        }

        /// <summary>
        /// Raises the Resize event.
        /// Note: this method may be called before the OpenGL context is ready.
        /// Check that IsHandleCreated is true before using any OpenGL methods.
        /// </summary>
        /// <param name="e">A System.EventArgs that contains the event data.</param>
        protected override void OnResize(EventArgs e)
        {
            // Do not raise OnResize event before the handle and context are created.
            if(!IsHandleCreated) {
                _resizeEventSuppressed = true;
                return;
            }
            _context?.Update(Implementation.WindowInfo);
            base.OnResize(e);
        }

        /// <summary>
        /// Raises the ParentChanged event.
        /// </summary>
        /// <param name="e">A System.EventArgs that contains the event data.</param>
        protected override void OnParentChanged(EventArgs e)
        {
            _context?.Update(Implementation.WindowInfo);
            base.OnParentChanged(e);
        }

        /// <summary>
        /// Validate <see cref="_implementation"/> and <see cref="_context"/>.
        /// They are set as valid value after calling this method.</summary>
        private void ValidateState()
        {
            if(IsDisposed) { throw new ObjectDisposedException(GetType().Name); }
            if(!IsHandleCreated) { CreateControl(); }
            if(_implementation == null || _context == null || _context.IsDisposed) {
                RecreateHandle();
            }
        }


        private interface IGLControlImpl
        {
            IGraphicsContext CreateContext(int major, int minor, GraphicsContextFlags flags);
            bool IsIdle { get; }
            IWindowInfo WindowInfo { get; }
        }

        class DummyGLControlImpl : IGLControlImpl
        {
            public IGraphicsContext CreateContext(int major, int minor, GraphicsContextFlags flags)
                => new GraphicsContext(null, null);

            public bool IsIdle => false;

            public IWindowInfo WindowInfo => Utilities.CreateDummyWindowInfo();
        }

        class WinGLControlImpl : IGLControlImpl
        {
            [SuppressUnmanagedCodeSecurity]
            [DllImport("User32.dll")]
            static extern bool PeekMessage(ref MSG msg, IntPtr hWnd, int messageFilterMin, int messageFilterMax, int flags);

            private MSG _msg;
            private GraphicsMode _graphicsMode;

            public bool IsIdle => !PeekMessage(ref _msg, IntPtr.Zero, 0, 0, 0);

            // This method forces the creation of the control. Beware of this side-effect!
            public IWindowInfo WindowInfo { get; }

            public WinGLControlImpl(GraphicsMode mode, IntPtr controlHandle)
            {
                _graphicsMode = mode;
                WindowInfo = Utilities.CreateWindowsWindowInfo(controlHandle);
            }

            public IGraphicsContext CreateContext(int major, int minor, GraphicsContextFlags flags)
                => new GraphicsContext(_graphicsMode, WindowInfo, major, minor, flags);

            struct MSG
            {
                public IntPtr HWnd;
                public uint Message;
                public IntPtr WParam;
                public IntPtr LParam;
                public uint Time;
                public POINT Point;

                public override string ToString()
                    => $"msg=0x{(int)Message:x} ({Message}) hwnd=0x{HWnd.ToInt32():x}, wparam=0x{WParam.ToInt32():x}, lparam=0x{LParam.ToInt32():x} pt=0x{Point:x}";
            }

            struct POINT
            {
                public int X;
                public int Y;

                public POINT(int x, int y) => (X, Y) = (x, y);

                public Point ToPoint() => new Point(X, Y);

                public override string ToString() => $"({X}, {Y})";
            }
        }
    }
}
