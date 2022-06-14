#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Elffy.UI
{
    public interface ITextContent
    {
        string? Text { get; set; }
        string? FontFamily { get; set; }
        int FontSize { get; set; }
        ColorByte Foreground { get; set; }
        HorizontalTextAlignment TextAlignment { get; set; }
        Event<(ITextContent Sender, string PropertyName)> TextContentChanged { get; }
    }

    internal struct TextContentImpl : ITextContent
    {
        private ITextContent _owner;
        private string? _text;
        private string? _fontFamily;
        private int _fontSize;
        private ColorByte _foreground;
        private HorizontalTextAlignment _textAlignment;

        private EventRaiser<(ITextContent Sender, string PropertyName)>? _propertyChanged;
        public Event<(ITextContent Sender, string PropertyName)> TextContentChanged => new(ref _propertyChanged);

        public string? Text { get => _text; set => SetValue(ref _text, value); }

        public string? FontFamily { get => _fontFamily; set => SetValue(ref _fontFamily, value); }

        public int FontSize { get => _fontSize; set => SetValue(ref _fontSize, value); }

        public ColorByte Foreground { get => _foreground; set => SetValue(ref _foreground, value); }

        public HorizontalTextAlignment TextAlignment { get => _textAlignment; set => SetValue(ref _textAlignment, value); }

        [Obsolete("Don't use default constructor.")]
        public TextContentImpl() => throw new NotSupportedException("Don't use default constructor.");

        public TextContentImpl(ITextContent owner)
        {
            _owner = owner;
            _propertyChanged = null;
            _foreground = ColorByte.Black;
            _text = null;
            _fontFamily = null;
            _fontSize = 14;
            _textAlignment = HorizontalTextAlignment.Center;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetValue<TValue>(ref TValue field, TValue value, [CallerMemberName] string callerName = "")
        {
            if(EqualityComparer<TValue>.Default.Equals(field, value)) { return; }
            field = value;
            _propertyChanged?.Raise((_owner, callerName));
        }
    }
}
