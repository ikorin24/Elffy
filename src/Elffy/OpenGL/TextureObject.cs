#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using OpenToolkit.Graphics.OpenGL;
using Elffy.Core;
using Elffy.Imaging;
using System.Drawing;
using System.Drawing.Imaging;
using PixelFormat = System.Drawing.Imaging.PixelFormat;
using TKPixelFormat = OpenToolkit.Graphics.OpenGL.PixelFormat;

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

        internal static void Bind(in TextureObject to, TextureUnit textureUnit = TextureUnit.Texture0)
        {
            GL.ActiveTexture(textureUnit);
            GL.BindTexture(TextureTarget.Texture2D, to._texture);
            _binded[textureUnit - TextureUnit.Texture0] = to;
        }

        internal static void Unbind(TextureUnit textureUnit = TextureUnit.Texture0)
        {
            Bind(Empty, textureUnit);
        }

        internal static void Delete(ref TextureObject to)
        {
            if(!to.IsEmpty) {
                GL.DeleteTexture(to._texture);
                Unsafe.AsRef(to._texture) = default;
            }
        }

        internal static void Load(in TextureObject to, Bitmap bitmap)
        {
            if(bitmap is null) { throw new ArgumentNullException(nameof(bitmap)); }
            Bind(to);
            using var pixels = bitmap.GetPixels(ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
                          pixels.Width, pixels.Height, 0, TKPixelFormat.Bgra, PixelType.UnsignedByte, pixels.Ptr);
        }

        public static TextureObject GetBinded(TextureUnit textureUnit = TextureUnit.Texture0)
        {
            return _binded[textureUnit - TextureUnit.Texture0];
        }

        public override string ToString() => _texture.ToString();

        public override bool Equals(object? obj) => obj is TextureObject to && Equals(to);

        public bool Equals(TextureObject other) => _texture == other._texture;

        public override int GetHashCode() => HashCode.Combine(_texture);

        public static bool operator ==(TextureObject left, TextureObject right) => left.Equals(right);

        public static bool operator !=(TextureObject left, TextureObject right) => !(left == right);
    }
}
