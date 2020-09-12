#nullable enable
using System;
using Elffy.Core;
using OpenToolkit.Graphics.OpenGL4;

namespace Elffy.OpenGL
{
    /// <summary>Render Buffer Object of OpenGL</summary>
    public readonly struct RBO : IEquatable<RBO>
    {
        private readonly int _rbo;

        internal int Value => _rbo;
        public bool IsEmpty => _rbo == Consts.NULL;

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

        internal static void SetStorage(int width, int height)
        {
            GLAssert.EnsureContext();
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.Depth24Stencil8, width, height);
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
    }
}
