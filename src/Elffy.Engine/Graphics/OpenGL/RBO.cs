#nullable enable
using System;
using System.Diagnostics;
using OpenTK.Graphics.OpenGL4;

namespace Elffy.Graphics.OpenGL
{
    /// <summary>Render Buffer Object of OpenGL</summary>
    [DebuggerDisplay("RBO={_rbo}")]
    public readonly struct RBO : IEquatable<RBO>
    {
        private readonly int _rbo;

        internal int Value => _rbo;

        /// <summary>Get whether the render buffer object is empty or not</summary>
        public bool IsEmpty => _rbo == 0;

        /// <summary>Get empty <see cref="RBO"/>. (that means 0)</summary>
        public static RBO Empty => new RBO();

        private RBO(int rbo)
        {
            _rbo = rbo;
        }

        /// <summary>Call glBindRenderbuffer</summary>
        /// <param name="rbo">render buffer object</param>
        public static void Bind(in RBO rbo)
        {
            GLAssert.EnsureContext();
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, rbo._rbo);
        }

        /// <summary>Call glBindRenderbuffer with 0.</summary>
        public static void Unbind()
        {
            GLAssert.EnsureContext();
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);
        }

        /// <summary>Create new <see cref="RBO"/> (Call glGenRenderbuffer)</summary>
        /// <returns>new <see cref="RBO"/></returns>
        public static RBO Create()
        {
            GLAssert.EnsureContext();
            return new RBO(GL.GenRenderbuffer());
        }

        /// <summary>Call glRenderbufferStorage</summary>
        /// <param name="size">size of storage</param>
        /// <param name="type">type of storage</param>
        public static void Storage(in Vector2i size, StorageType type)
        {
            GLAssert.EnsureContext();
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, (RenderbufferStorage)type, size.X, size.Y);
        }

        /// <summary>Call glDeleteRenderbuffer</summary>
        /// <param name="rbo">render buffer object</param>
        public static void Delete(ref RBO rbo)
        {
            GLAssert.EnsureContext();
            GL.DeleteRenderbuffer(rbo._rbo);
            rbo = default;
        }

        public override bool Equals(object? obj) => obj is RBO rbo && Equals(rbo);

        public bool Equals(RBO other) => _rbo == other._rbo;

        public override int GetHashCode() => _rbo.GetHashCode();

        public enum StorageType
        {
            /// <summary>GL_RGBA32F, Each chanel is 32 bits floating point value.</summary>
            Rgba32f = RenderbufferStorage.Rgba32f,

            /// <summary>GL_DEPTH_COMPONENT, Depth buffer that the driver chooses its precision.</summary>
            DepthComponent = RenderbufferStorage.DepthComponent,
            /// <summary>GL_DEPTH_COMPONENT16, Depth buffer of 16 bits precision.</summary>
            DepthComponent16 = RenderbufferStorage.DepthComponent16,
            /// <summary>GL_DEPTH_COMPONENT24, Depth buffer of 24 bits precision.</summary>
            DepthComponent24 = RenderbufferStorage.DepthComponent24,
            /// <summary>GL_DEPTH_COMPONENT32, Depth buffer of 32 bits precision.</summary>
            DepthComponent32 = RenderbufferStorage.DepthComponent32,

            /// <summary>GL_STENCIL_INDEX1</summary>
            Stencil1 = RenderbufferStorage.StencilIndex1,
            /// <summary>GL_STENCIL_INDEX4</summary>
            Stencil4 = RenderbufferStorage.StencilIndex4,
            /// <summary>GL_STENCIL_INDEX8</summary>
            Stencil8 = RenderbufferStorage.StencilIndex8,
            /// <summary>GL_STENCIL_INDEX16</summary>
            Stencil16 = RenderbufferStorage.StencilIndex16,

            /// <summary>GL_DEPTH24_STENCIL8</summary>
            Depth24Stencil8 = RenderbufferStorage.Depth24Stencil8,
        }
    }
}
