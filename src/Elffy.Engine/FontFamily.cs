#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using SkiaSharp;

namespace Elffy
{
    public readonly struct FontFamily : IEquatable<FontFamily>
    {
        private readonly SKTypeface? _skTypeface;

        public static FontFamily None => default;

        internal SKTypeface? SkTypeface => _skTypeface;

        [Obsolete("Don't use default constructor.", true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public FontFamily() => throw new NotSupportedException("Don't use default constructor.");

        internal FontFamily(SKTypeface sKTypeface)
        {
            _skTypeface = sKTypeface;
        }

        public override bool Equals(object? obj) => obj is FontFamily family && Equals(family);

        public bool Equals(FontFamily other) => EqualityComparer<SKTypeface?>.Default.Equals(_skTypeface, other._skTypeface);

        public override int GetHashCode() => _skTypeface?.GetHashCode() ?? 0;

        public static bool operator ==(FontFamily left, FontFamily right) => left.Equals(right);

        public static bool operator !=(FontFamily left, FontFamily right) => !(left == right);
    }

    internal sealed class FontFamilies
    {
        private readonly Dictionary<string, SKTypeface> _dic = new Dictionary<string, SKTypeface>();

        public static FontFamilies Instance { get; } = new FontFamilies();  // TODO: 

        private FontFamilies()
        {
        }

        public FontFamily GetFontFamilyOrDefault(string? familyName)
        {
            if(familyName != null && _dic.TryGetValue(familyName, out var typeface)) {
                return new FontFamily(typeface);
            }
            return FontFamily.None;
        }

        public bool TryGetFontFamily(string? familyName, [MaybeNullWhen(false)] out FontFamily fontFamily)
        {
            if(familyName == null) {
                fontFamily = FontFamily.None;
                return true;
            }

            if(_dic.TryGetValue(familyName, out var typeface)) {
                fontFamily = new FontFamily(typeface);
                return true;
            }
            fontFamily = FontFamily.None;
            return false;
        }

        public unsafe bool Register(ResourceFile file, out string familyName)
        {
            if(file.IsNone) { throw new ArgumentException("file is none.", nameof(file)); }
            using var stream = file.GetStream();

            SKTypeface tf;
            if(stream.CanSeek && stream.Length <= int.MaxValue) {
                var size = (int)stream.Length;
                using var data = SKData.Create(size);
                stream.Read(new Span<byte>((void*)data.Data, size));
                tf = SKTypeface.FromData(data, 0);
            }
            else {
                using var data = SKData.Create(stream);
                tf = SKTypeface.FromData(data, 0);
            }
            familyName = tf.FamilyName;
            return _dic.TryAdd(tf.FamilyName, tf);
        }

        internal void Release()
        {
            _dic.Clear();
        }
    }
}
