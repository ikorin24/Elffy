#nullable enable
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
            CornerRadius = new Vector4(2);
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
