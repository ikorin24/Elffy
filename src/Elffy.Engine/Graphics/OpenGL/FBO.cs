#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Elffy.Graphics.OpenGL
{
    /// <summary>Frame Buffer Object of OpenGL</summary>
    [DebuggerDisplay("FBO={_fbo}")]
    public readonly struct FBO : IEquatable<FBO>
    {
        private static readonly Dictionary<IntPtr, FBO> _readBinded = new Dictionary<IntPtr, FBO>();
        private static readonly Dictionary<IntPtr, FBO> _drawBinded = new Dictionary<IntPtr, FBO>();

        private readonly int _fbo;

        internal int Value => _fbo;

        public bool IsEmpty => _fbo == 0;

        public static FBO Empty => default;

        public unsafe static FBO CurrentReadBinded
        {
            get
            {
                var c = (IntPtr)GLFW.GetCurrentContext();
                return _readBinded.TryGetValue(c, out var fbo) ? fbo : Empty;
            }
        }
        public unsafe static FBO CurrentDrawBinded
        {
            get
            {
                var c = (IntPtr)GLFW.GetCurrentContext();
                return _drawBinded.TryGetValue(c, out var fbo) ? fbo : Empty;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private FBO(int fbo)
        {
            _fbo = fbo;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Bind(in FBO fbo, Target target)
        {
            GLAssert.EnsureContext();

            var context = (IntPtr)GLFW.GetCurrentContext();
            if(target != Target.Read) {
                _drawBinded[context] = fbo;
            }
            if(target != Target.Draw) {
                _readBinded[context] = fbo;
            }

            GL.BindFramebuffer((FramebufferTarget)target, fbo._fbo);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Unbind(Target target)
        {
            GLAssert.EnsureContext();

            var context = (IntPtr)GLFW.GetCurrentContext();
            if(target != Target.Read) {
                _drawBinded[context] = Empty;
            }
            if(target != Target.Draw) {
                _readBinded[context] = Empty;
            }

            GL.BindFramebuffer((FramebufferTarget)target, 0);
        }

        /// <summary>Call glFreameBufferTexture2D with GL_COLOR_ATTACHMENT`n` (`n` is <paramref name="colorAttachmentNum"/>)</summary>
        /// <param name="to">texture object</param>
        /// <param name="colorAttachmentNum">color attachment number</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetTexture2DColorAttachment(in TextureObject to, int colorAttachmentNum)
        {
            GLAssert.EnsureContext();
            var attachment = FramebufferAttachment.ColorAttachment0 + colorAttachmentNum;
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, attachment, TextureTarget.Texture2D, to.Value, 0);
        }

        /// <summary>Call glFreameBufferTexture with GL_DEPTH_ATTACHMENT</summary>
        /// <param name="to">texture object</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetTextureDepthAttachment(in TextureObject to)
        {
            GLAssert.EnsureContext();
            GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, to.Value, 0);
        }

        /// <summary>Call glFramebufferRenderbuffer with GL_DEPTH_STENCIL_ATTACHMENT</summary>
        /// <param name="rbo">render buffer object</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetRenderBufferDepthStencilAttachment(in RBO rbo)
        {
            GLAssert.EnsureContext();
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, RenderbufferTarget.Renderbuffer, rbo.Value);
        }

        /// <summary>Call glCheckFramebufferStatus</summary>
        /// <param name="error">error message</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CheckStatus(out string error)
        {
            GLAssert.EnsureContext();
            var status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            var isError = status != FramebufferErrorCode.FramebufferComplete;
            error = isError ? status.ToString() : "";
            return !isError;
        }

        public static void ThrowIfInvalidStatus()
        {
            if(CheckStatus(out var error) == false) {
                throw new InvalidOperationException(error);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FBO Create()
        {
            GLAssert.EnsureContext();
            return new FBO(GL.GenFramebuffer());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Delete(ref FBO fbo)
        {
            GLAssert.EnsureContext();
            GL.DeleteFramebuffer(fbo._fbo);
            fbo = default;
        }

        internal static PreservedState PreserveCurrentBinded() => new PreservedState(CurrentReadBinded, CurrentDrawBinded);

        internal readonly ref struct PreservedState
        {
            private readonly FBO _read;
            private readonly FBO _draw;

            [Obsolete("Don't use default constructor.", true)]
            public PreservedState() => throw new NotSupportedException("Don't use default constructor.");

            public PreservedState(FBO read, FBO draw)
            {
                _read = read;
                _draw = draw;
            }

            public void Dispose()
            {
                Bind(_read, Target.Read);
                Bind(_draw, Target.Draw);
            }
        }

        public override string ToString() => _fbo.ToString();

        public override bool Equals(object? obj) => obj is FBO fBO && Equals(fBO);

        public bool Equals(FBO other) => _fbo == other._fbo;

        public static bool operator ==(FBO left, FBO right) => left.Equals(right);

        public static bool operator !=(FBO left, FBO right) => !(left == right);

        public override int GetHashCode() => _fbo.GetHashCode();

        public enum Target
        {
            FrameBuffer = FramebufferTarget.Framebuffer,
            Draw = FramebufferTarget.DrawFramebuffer,
            Read = FramebufferTarget.ReadFramebuffer,
        }
    }
}
