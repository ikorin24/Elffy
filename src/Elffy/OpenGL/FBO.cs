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
        public static void Bind(in FBO fbo)
        {
            GLAssert.EnsureContext();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, fbo._fbo);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Unbind()
        {
            GLAssert.EnsureContext();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, Consts.NULL);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void SetTexture2DBuffer(in TextureObject to)
        {
            GLAssert.EnsureContext();
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, to.Value, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void SetRenderBuffer(in RBO rbo)
        {
            GLAssert.EnsureContext();
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, RenderbufferTarget.Renderbuffer, rbo.Value);
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
    }
}
