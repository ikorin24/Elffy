#nullable enable
using System;
using System.ComponentModel;

namespace Elffy.Shading
{
    public readonly ref struct ComputeShaderContext
    {
        private readonly IHostScreen _screen;

        public IHostScreen Screen => _screen;
        public Vector2i ScreenSize => _screen.FrameBufferSize;

        [Obsolete("Don't use defaut constructor.", true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ComputeShaderContext() => throw new NotSupportedException("Don't use defaut constructor.");

        internal ComputeShaderContext(IHostScreen screen)
        {
            _screen = screen;
        }
    }
}
