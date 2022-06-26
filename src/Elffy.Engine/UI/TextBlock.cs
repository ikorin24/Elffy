#nullable enable

namespace Elffy.UI
{
    public class TextBlock : Control, ITextContent
    {
        private TextContentImpl _textContentImpl;

        public string? Text
        {
            get => _textContentImpl.Text;
            set => _textContentImpl.Text = value;
        }

        public string? FontFamily
        {
            get => _textContentImpl.FontFamily;
            set => _textContentImpl.FontFamily = value;
        }

        public int FontSize
        {
            get => _textContentImpl.FontSize;
            set => _textContentImpl.FontSize = value;
        }

        public Color4 Foreground
        {
            get => _textContentImpl.Foreground;
            set => _textContentImpl.Foreground = value;
        }

        public HorizontalAlignment TextAlignment
        {
            get => _textContentImpl.TextAlignment;
            set => _textContentImpl.TextAlignment = value;
        }

        public Event<(ITextContent Sender, string PropertyName)> TextContentChanged => _textContentImpl.TextContentChanged;

        public VerticalAlignment VerticalTextAlignment
        {
            get => _textContentImpl.VerticalTextAlignment;
            set => _textContentImpl.VerticalTextAlignment = value;
        }

        static TextBlock()
        {
            ControlShaderSelector.SetDefault<TextBlock>(() => new TextBlockDefaultShader());
        }

        public TextBlock()
        {
            _textContentImpl = new TextContentImpl(this);
        }
    }

    internal sealed class TextBlockDefaultShader : TextContentShaderBase
    {
        public TextBlockDefaultShader()
        {
        }
    }
}
