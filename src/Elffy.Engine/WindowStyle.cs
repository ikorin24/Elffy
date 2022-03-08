#nullable enable

namespace Elffy
{
    public enum WindowStyle
    {
        Default = 0,
        Fullscreen = 1,
        FixedWindow = 2
    }

    public enum WindowBorderStyle
    {
        Default = 0,
        NoBorder = 1,
    }

    public enum WindowVisibility
    {
        Visible = 0,
        Hidden = 1,
    }

    public record struct WindowConfig
    {
        public int Width { get; init; }
        public int Height { get; init; }
        public float FrameRate { get; init; }
        public string Title { get; init; } = "";
        public ResourceFile Icon { get; init; }
        public WindowStyle Style { get; init; }
        public WindowBorderStyle BorderStyle { get; init; }
        public WindowVisibility Visibility { get; init; }
    }
}
