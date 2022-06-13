#nullable enable
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Elffy.UI
{
    /// <summary>Button class which fires event on mouse click.</summary>
    public class Button : Executable, ITextContent
    {
        private bool _isEnabled;
        private bool _isEnabledChanged;
        private TextContentImpl _textContentImpl;

        public string? Text
        {
            get => _textContentImpl.Text;
            set => _textContentImpl.Text = value;
        }

        public int FontSize
        {
            get => _textContentImpl.FontSize;
            set => _textContentImpl.FontSize = value;
        }

        public ColorByte Foreground
        {
            get => _textContentImpl.Foreground;
            set => _textContentImpl.Foreground = value;
        }

        public HorizontalTextAlignment TextAlignment
        {
            get => _textContentImpl.TextAlignment;
            set => _textContentImpl.TextAlignment = value;
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if(_isEnabled == value) { return; }
                _isEnabled = value;
                _isEnabledChanged = true;
            }
        }

        public Event<(ITextContent Sender, string PropertyName)> TextContentChanged => _textContentImpl.PropertyChanged;

        /// <summary>Create new <see cref="Button"/></summary>
        public Button()
        {
            _isEnabled = true;
            _textContentImpl = new TextContentImpl(this);
            Text = "button";  // TODO:
            Shader = new ButtonDefaultShader(); // TODO:
        }

        protected override void OnUIEvent()
        {
            var isEnabledChanged = _isEnabledChanged;
            _isEnabledChanged = false;
            if(isEnabledChanged && _isEnabled == false) {
                ForceCancelExecutableEventFlow();
            }
            base.OnUIEvent();
        }
    }

    public interface ITextContent
    {
        string? Text { get; set; }
        int FontSize { get; set; }
        ColorByte Foreground { get; set; }
        HorizontalTextAlignment TextAlignment { get; set; }
        Event<(ITextContent Sender, string PropertyName)> TextContentChanged { get; }
    }

    internal struct TextContentImpl
    {
        private ITextContent _owner;
        private string? _text;
        private int _fontSize;
        private ColorByte _foreground;
        private HorizontalTextAlignment _textAlignment;

        private EventRaiser<(ITextContent Sender, string PropertyName)>? _propertyChanged;
        public Event<(ITextContent Sender, string PropertyName)> PropertyChanged => new(ref _propertyChanged);

        public string? Text
        {
            get => _text;
            set => SetValue(ref _text, value);
        }

        public int FontSize
        {
            get => _fontSize;
            set => SetValue(ref _fontSize, value);
        }

        public ColorByte Foreground
        {
            get => _foreground;
            set => SetValue(ref _foreground, value);
        }

        public HorizontalTextAlignment TextAlignment
        {
            get => _textAlignment;
            set => SetValue(ref _textAlignment, value);
        }

        public TextContentImpl(ITextContent owner)
        {
            _owner = owner;
            _propertyChanged = null;
            _foreground = ColorByte.Black;
            _text = null;
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
