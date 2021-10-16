#nullable enable

namespace Elffy.UI
{
    /// <summary>UI layout execution type.</summary>
    public enum LayoutExecutionType : byte
    {
        Adaptive = 0,
        /// <summary>Execute layouting explicitly by calling the method.</summary>
        Explicit,
        /// <summary>Execute layouting every frame.</summary>
        EveryFrame,
    }
}
