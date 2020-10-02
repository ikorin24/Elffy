#nullable enable
using System;
using System.Diagnostics;
using OpenTK.Graphics.OpenGL4;
using Elffy.Core;

namespace Elffy.OpenGL
{
    [DebuggerDisplay("Texture={_texture}")]
    public readonly struct TextureObject : IEquatable<TextureObject>
    {
        private readonly int _texture;

        internal int Value => _texture;

        public bool IsEmpty => _texture == Consts.NULL;

        public static TextureObject Empty => new TextureObject();

        private TextureObject(int texture)
        {
            _texture = texture;
        }

        internal static TextureObject Create()
        {
            GLAssert.EnsureContext();
            return new TextureObject(GL.GenTexture());
        }

        internal static void Bind2D(in TextureObject to, TextureUnitNumber textureUnit)
        {
            var glTextureUnit = (TextureUnit)((int)TextureUnit.Texture0 + textureUnit);

            GLAssert.EnsureContext();
            GL.ActiveTexture(glTextureUnit);
            GL.BindTexture(TextureTarget.Texture2D, to._texture);
        }

        internal static void Bind1D(in TextureObject to, TextureUnitNumber textureUnit)
        {
            var glTextureUnit = (TextureUnit)((int)TextureUnit.Texture0 + textureUnit);

            GLAssert.EnsureContext();
            GL.ActiveTexture(glTextureUnit);
            GL.BindTexture(TextureTarget.Texture1D, to._texture);
        }

        internal static void Unbind2D(TextureUnitNumber textureUnit)
        {
            Bind2D(Empty, textureUnit);
        }

        internal static void Unbind1D(TextureUnitNumber textureUnit)
        {
            Bind1D(Empty, textureUnit);
        }

        internal static void Delete(ref TextureObject to)
        {
            if(to._texture != Consts.NULL) {
                GLAssert.EnsureContext();
                GL.DeleteTexture(to._texture);
                to = default;
            }
        }

        public override string ToString() => _texture.ToString();

        public override bool Equals(object? obj) => obj is TextureObject to && Equals(to);

        public bool Equals(TextureObject other) => _texture == other._texture;

        public override int GetHashCode() => _texture.GetHashCode();
    }
}
