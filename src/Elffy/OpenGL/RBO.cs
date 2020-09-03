#nullable enable
using System;
using System.Runtime.CompilerServices;
using Elffy.Core;
using OpenToolkit.Graphics.OpenGL4;

namespace Elffy.OpenGL
{
    /// <summary>Render Buffer Object of OpenGL</summary>
    public readonly struct RBO : IEquatable<RBO>
    {
        private readonly int _rbo;

        internal int Value => _rbo;

        private RBO(int rbo)
        {
            _rbo = rbo;
        }

        public static void Bind(in RBO rbo)
        {
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, rbo._rbo);
        }

        public static void Unbind()
        {
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);
        }

        internal static RBO Create()
        {
            return new RBO(GL.GenRenderbuffer());
        }

        internal static void SetStorage(int width, int height)
        {
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.Depth24Stencil8, width, height);
        }

        internal static void Delete(ref RBO rbo)
        {
            GL.DeleteRenderbuffer(rbo._rbo);
            Unsafe.AsRef(rbo._rbo) = Consts.NULL;
        }

        public override bool Equals(object? obj) => obj is RBO rBO && Equals(rBO);

        public bool Equals(RBO other) => _rbo == other._rbo;

        public override int GetHashCode() => HashCode.Combine(_rbo);
    }
}
