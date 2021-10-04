#nullable enable

namespace Elffy.UI
{
    /// <summary>Layout horizontal alignment</summary>
    public enum HorizontalAlignment : byte
    {
        /// <summary>center alignment</summary>
        Center,
        /// <summary>left alignment</summary>
        Left,
        /// <summary>right alignment</summary>
        Right,
    }

    /// <summary>Layout vertical alignment</summary>
    public enum VerticalAlignment : byte
    {
        /// <summary>center alignment</summary>
        Center,
        /// <summary>top alignment</summary>
        Top,
        /// <summary>bottom alignment</summary>
        Bottom,
    }

    public enum TransformOrigin : byte
    {
        LeftTop,
        LeftCenter,
        LeftBottom,
        CenterTop,
        Center,
        CenterBottom,
        RightTop,
        RightCenter,
        RightBottom,
    }
}
