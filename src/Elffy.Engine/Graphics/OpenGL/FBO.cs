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

        public bool IsEmpty => _fbo == Consts.NULL;

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

            GL.BindFramebuffer((FramebufferTarget)target, Consts.NULL);
        }

        /// <summary>Call glFreameBufferTexture2D</summary>
        /// <param name="to">texture object</param>
        /// <param name="attachment">attachment type</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetTexture2DBuffer(in TextureObject to, Attachment attachment)
        {
            GLAssert.EnsureContext();
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, (FramebufferAttachment)attachment, TextureTarget.Texture2D, to.Value, 0);
        }

        /// <summary>Call glFramebufferRenderbuffer</summary>
        /// <param name="rbo">render buffer object</param>
        /// <param name="attachment">attachment type</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetRenderBuffer(in RBO rbo, Attachment attachment)
        {
            GLAssert.EnsureContext();
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, (FramebufferAttachment)attachment, RenderbufferTarget.Renderbuffer, rbo.Value);
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

        public override string ToString() => _fbo.ToString();

        public override bool Equals(object? obj) => obj is FBO fBO && Equals(fBO);

        public bool Equals(FBO other) => _fbo == other._fbo;

        public override int GetHashCode() => _fbo.GetHashCode();

        public enum Target
        {
            FrameBuffer = FramebufferTarget.Framebuffer,
            Draw = FramebufferTarget.DrawFramebuffer,
            Read = FramebufferTarget.ReadFramebuffer,
        }

        public enum Attachment
        {
            DepthAttachment = FramebufferAttachment.DepthAttachment,
            StencilAttachment = FramebufferAttachment.StencilAttachment,
            ColorAttachment0 = FramebufferAttachment.ColorAttachment0,
            ColorAttachment1 = FramebufferAttachment.ColorAttachment1,
            ColorAttachment2 = FramebufferAttachment.ColorAttachment2,
            ColorAttachment3 = FramebufferAttachment.ColorAttachment3,
            ColorAttachment4 = FramebufferAttachment.ColorAttachment4,
            ColorAttachment5 = FramebufferAttachment.ColorAttachment5,
            ColorAttachment6 = FramebufferAttachment.ColorAttachment6,
            ColorAttachment7 = FramebufferAttachment.ColorAttachment7,
            DepthStencilAttachment = FramebufferAttachment.DepthStencilAttachment,
        }
    }
}
