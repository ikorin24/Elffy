#nullable enable
using Elffy.Core;
using Elffy.OpenGL;
using OpenTK.Graphics.OpenGL4;
using System.Runtime.CompilerServices;

namespace Elffy.Shading
{
    internal readonly ref struct FrameBufferScope
    {
        private readonly PostProcessCompiled? _compiled;
        private readonly FBO _currentFbo;
        private readonly FBO _targetFbo;
        private readonly Vector2i _screenSize;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private FrameBufferScope(PostProcessCompiled? compiled, in Vector2i screenSize)
        {
            // ctor for root scope.
            _compiled = compiled;
            _screenSize = screenSize;
            _targetFbo = FBO.Empty;     // Target fbo of root scope is 0, which means rendering to screen.
            if(compiled is null) {
                // If post process is null, this does not change fbo.
                // And do nothing on disposing.
                _currentFbo = FBO.Empty;
            }
            else {
                // Change fbo for post process.
                // (and restore it when the scope is disposed.)
                compiled.EnsureBuffer(_screenSize);
                _targetFbo = FBO.Empty;
                _currentFbo = compiled.FBO;
                FBO.Bind(_currentFbo);
            }
            // Root scope always clear buffer
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private FrameBufferScope(PostProcessCompiled? compiled, in FrameBufferScope parentScope)
        {
            // ctor for inner scope.
            _compiled = compiled;
            _screenSize = parentScope._screenSize;
            if(compiled is null) {
                // If post process is null, this does not change fbo.
                // And do nothing on disposing.
                _targetFbo = parentScope._targetFbo;
                _currentFbo = parentScope._currentFbo;
            }
            else {
                // Change fbo for post process.
                // (and restore current fbo of parent scope when the scope is disposed.)
                compiled.EnsureBuffer(_screenSize);
                _targetFbo = parentScope._currentFbo;
                _currentFbo = compiled.FBO;
                FBO.Bind(_currentFbo);
                // Clear buffer of switched fbo.
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal FrameBufferScope NewScope(PostProcessCompiled? compiled)
        {
            return new FrameBufferScope(compiled, this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static FrameBufferScope RootScope(PostProcessCompiled? compiled, in Vector2i screenSizes)
        {
            return new FrameBufferScope(compiled, screenSizes);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            // Restore fbo and render post process.
            // Do nothing when post process is null.
            if(_compiled is null == false) {
                RenderPostProcess();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void RenderPostProcess()
        {
            FBO.Bind(_targetFbo);
            var depthTestEnabled = GL.IsEnabled(EnableCap.DepthTest);
            GL.Disable(EnableCap.DepthTest);
            _compiled!.Render();
            if(depthTestEnabled) {
                GL.Enable(EnableCap.DepthTest);
            }
        }
    }
}
