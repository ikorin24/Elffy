#nullable enable
using System;
using System.Diagnostics;
using Elffy.Core;
using OpenTK.Graphics.OpenGL4;

namespace Elffy.OpenGL
{
    /// <summary>Render Buffer Object of OpenGL</summary>
    [DebuggerDisplay("RBO={_rbo}")]
    public readonly struct RBO : IEquatable<RBO>
    {
        private readonly int _rbo;

        internal int Value => _rbo;
        public bool IsEmpty => _rbo == Consts.NULL;

        public static RBO Empty => new RBO();

        private RBO(int rbo)
        {
            _rbo = rbo;
        }

        public static void Bind(in RBO rbo)
        {
            GLAssert.EnsureContext();
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, rbo._rbo);
        }

        public static void Unbind()
        {
            GLAssert.EnsureContext();
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);
        }

        internal static RBO Create()
        {
            GLAssert.EnsureContext();
            return new RBO(GL.GenRenderbuffer());
        }

        internal static void SetStorage(int width, int height, StorageType type)
        {
            GLAssert.EnsureContext();
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, (RenderbufferStorage)type, width, height);
        }

        internal static void Delete(ref RBO rbo)
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
            /// <summary>each color chanels of RGBA are 32bits</summary>
            Rgba32f = RenderbufferStorage.Rgba32f,

            /// <summary>16bits depth</summary>
            Depth16 = RenderbufferStorage.DepthComponent16,
            Depth24 = RenderbufferStorage.DepthComponent24,
            Depth32 = RenderbufferStorage.DepthComponent32,

            Stencil1 = RenderbufferStorage.StencilIndex1,
            Stencil4 = RenderbufferStorage.StencilIndex4,
            Stencil8 = RenderbufferStorage.StencilIndex8,
            Stencil16 = RenderbufferStorage.StencilIndex16,

            Stencil24Stencil8 = RenderbufferStorage.Depth24Stencil8,
        }
    }
}
