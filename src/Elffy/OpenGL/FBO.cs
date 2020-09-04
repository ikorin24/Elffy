#nullable enable
using System;
using System.Runtime.CompilerServices;
using Elffy.Core;
using OpenToolkit.Graphics.OpenGL4;

namespace Elffy.OpenGL
{
    /// <summary>Frame Buffer Object of OpenGL</summary>
    public readonly struct FBO : IEquatable<FBO>
    {
        private readonly int _fbo;

        public readonly bool IsEmpty => _fbo == Consts.NULL;

        private FBO(int fbo)
        {
            _fbo = fbo;
        }

        public static void Bind(in FBO fbo)
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, fbo._fbo);
        }

        public static void Unbind()
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, Consts.NULL);
        }

        internal static void SetTexture2DBuffer(in TextureObject to)
        {
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, to.Value, 0);
        }

        internal static void SetRenderBuffer(in RBO rbo)
        {
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, RenderbufferTarget.Renderbuffer, rbo.Value);
        }

        internal static bool CheckStatus(out FramebufferErrorCode status)
        {
            status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            return status == FramebufferErrorCode.FramebufferComplete;
        }

        internal static FBO Create()
        {
            return new FBO(GL.GenFramebuffer());
        }

        internal static void Delete(ref FBO fbo)
        {
            GL.DeleteFramebuffer(fbo._fbo);
            Unsafe.AsRef(fbo._fbo) = Consts.NULL;
        }

        public override bool Equals(object? obj) => obj is FBO fBO && Equals(fBO);

        public bool Equals(FBO other) => _fbo == other._fbo;

        public override int GetHashCode() => HashCode.Combine(_fbo);
    }
}
