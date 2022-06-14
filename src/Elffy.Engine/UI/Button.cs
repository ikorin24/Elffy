#nullable enable

namespace Elffy.UI
{
    /// <summary>Button class which fires event on mouse click.</summary>
    public class Button : Executable, ITextContent, IEnableControl
    {
        private bool _isEnabled;
        private bool _isEnabledChanged;
        private TextContentImpl _textContent;

        public string? Text { get => _textContent.Text; set => _textContent.Text = value; }

        public string? FontFamily { get => _textContent.FontFamily; set => _textContent.FontFamily = value; }

        public int FontSize { get => _textContent.FontSize; set => _textContent.FontSize = value; }

        public ColorByte Foreground { get => _textContent.Foreground; set => _textContent.Foreground = value; }

        public HorizontalTextAlignment TextAlignment { get => _textContent.TextAlignment; set => _textContent.TextAlignment = value; }

        public Event<(ITextContent Sender, string PropertyName)> TextContentChanged => _textContent.TextContentChanged;

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

        /// <summary>Create new <see cref="Button"/></summary>
        public Button()
        {
            _isEnabled = true;
            _textContent = new TextContentImpl(this);
            Shader = new ButtonDefaultShader(); // TODO:
            CornerRadius = new Vector4(3);
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
}
