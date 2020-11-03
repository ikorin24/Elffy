#nullable enable
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Elffy.Core;
using OpenTK.Graphics.OpenGL4;

namespace Elffy.OpenGL
{
    /// <summary>Frame Buffer Object of OpenGL</summary>
    [DebuggerDisplay("FBO={_fbo}")]
    public readonly struct FBO : IEquatable<FBO>
    {
        private readonly int _fbo;

        internal int Value => _fbo;

        public bool IsEmpty => _fbo == Consts.NULL;

        public static FBO Empty => default;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private FBO(int fbo)
        {
            _fbo = fbo;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Bind(in FBO fbo, Target target)
        {
            GLAssert.EnsureContext();
            GL.BindFramebuffer((FramebufferTarget)target, fbo._fbo);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Unbind(Target target)
        {
            GLAssert.EnsureContext();
            GL.BindFramebuffer((FramebufferTarget)target, Consts.NULL);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void SetTexture2DBuffer(in TextureObject to, Attachment attachment)
        {
            GLAssert.EnsureContext();
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, (FramebufferAttachment)attachment, TextureTarget.Texture2D, to.Value, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void SetRenderBuffer(in RBO rbo, Attachment attachment)
        {
            GLAssert.EnsureContext();
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, (FramebufferAttachment)attachment, RenderbufferTarget.Renderbuffer, rbo.Value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool CheckStatus(out FramebufferErrorCode status)
        {
            GLAssert.EnsureContext();
            status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            return status == FramebufferErrorCode.FramebufferComplete;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static FBO Create()
        {
            GLAssert.EnsureContext();
            return new FBO(GL.GenFramebuffer());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Delete(ref FBO fbo)
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
