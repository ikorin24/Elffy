﻿#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Elffy.UI
{
    public interface ITextContent
    {
        string? Text { get; set; }
        string? FontFamily { get; set; }
        int FontSize { get; set; }
        Color4 Foreground { get; set; }
        HorizontalAlignment TextAlignment { get; set; }
        VerticalAlignment VerticalTextAlignment { get; set; }
        [UnscopedRef]
        Event<(ITextContent Sender, string PropertyName)> TextContentChanged { get; }
    }

    internal struct TextContentImpl : ITextContent
    {
        private ITextContent _owner;
        private string? _text;
        private string? _fontFamily;
        private int _fontSize;
        private Color4 _foreground;
        private HorizontalAlignment _textAlignment;
        private VerticalAlignment _verticalTextAlignment;

        private EventSource<(ITextContent Sender, string PropertyName)> _propertyChanged;

        [UnscopedRef]
        public Event<(ITextContent Sender, string PropertyName)> TextContentChanged => _propertyChanged.Event;

        public string? Text { get => _text; set => SetValue(ref _text, value); }

        public string? FontFamily { get => _fontFamily; set => SetValue(ref _fontFamily, value); }

        public int FontSize { get => _fontSize; set => SetValue(ref _fontSize, value); }

        public Color4 Foreground { get => _foreground; set => SetValue(ref _foreground, value); }

        public HorizontalAlignment TextAlignment { get => _textAlignment; set => SetValue(ref _textAlignment, value); }
        public VerticalAlignment VerticalTextAlignment { get => _verticalTextAlignment; set => SetValue(ref _verticalTextAlignment, value); }

        [Obsolete("Don't use default constructor.")]
        public TextContentImpl() => throw new NotSupportedException("Don't use default constructor.");

        public TextContentImpl(ITextContent owner)
        {
            _owner = owner;
            _propertyChanged = new EventSource<(ITextContent Sender, string PropertyName)>();
            _foreground = Color4.Black;
            _text = null;
            _fontFamily = null;
            _fontSize = 14;
            _textAlignment = HorizontalAlignment.Center;
            _verticalTextAlignment = VerticalAlignment.Center;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetValue<TValue>(ref TValue field, TValue value, [CallerMemberName] string callerName = "")
        {
            if(EqualityComparer<TValue>.Default.Equals(field, value)) { return; }
            field = value;
            _propertyChanged.Invoke((_owner, callerName));
        }
    }
}
