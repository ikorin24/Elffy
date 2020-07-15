#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using OpenToolkit.Graphics.OpenGL;
using Elffy.Core;

namespace Elffy.OpenGL
{
    [DebuggerDisplay("Texture={_texture}")]
    public readonly struct TextureObject : IEquatable<TextureObject>
    {
        private readonly static TextureObject[] _binded = new TextureObject[32];

        private readonly int _texture;

        internal readonly int Value => _texture;

        public readonly bool IsEmpty => _texture == Consts.NULL;

        public static TextureObject Empty => new TextureObject();

        private TextureObject(int texture)
        {
            _texture = texture;
        }

        internal static TextureObject Create()
        {
            return new TextureObject(GL.GenTexture());
        }

        internal static void Bind2D(in TextureObject to, TextureUnitNumber textureUnit)
        {
            var glTextureUnit = (TextureUnit)((int)TextureUnit.Texture0 + textureUnit);

            GL.ActiveTexture(glTextureUnit);
            GL.BindTexture(TextureTarget.Texture2D, to._texture);
            _binded[(int)textureUnit] = to;
        }

        internal static void Bind1D(in TextureObject to, TextureUnitNumber textureUnit)
        {
            var glTextureUnit = (TextureUnit)((int)TextureUnit.Texture0 + textureUnit);

            GL.ActiveTexture(glTextureUnit);
            GL.BindTexture(TextureTarget.Texture1D, to._texture);
            _binded[(int)textureUnit] = to;
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
            if(!to.IsEmpty) {
                GL.DeleteTexture(to._texture);
                Unsafe.AsRef(to._texture) = default;
            }
        }

        public static TextureObject GetBinded(TextureUnitNumber textureUnit = TextureUnitNumber.Unit0)
        {
            return _binded[(int)textureUnit];
        }

        public override string ToString() => _texture.ToString();

        public override bool Equals(object? obj) => obj is TextureObject to && Equals(to);

        public bool Equals(TextureObject other) => _texture == other._texture;

        public override int GetHashCode() => HashCode.Combine(_texture);

        public static bool operator ==(TextureObject left, TextureObject right) => left.Equals(right);

        public static bool operator !=(TextureObject left, TextureObject right) => !(left == right);
    }
}
