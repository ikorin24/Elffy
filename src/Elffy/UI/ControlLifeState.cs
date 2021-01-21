#nullable enable

namespace Elffy.UI
{
    /// <summary>Life state of <see cref="Control"/></summary>
    public enum ControlLifeState : byte
    {
        /// <summary>control is new</summary>
        New,
        /// <summary>control is in the logical tree. (And in the visual tree.)</summary>
        InLogicalTree,
        /// <summary>control is only in the visual tree. (Not in the logical tree.)</summary>
        InVisualTreeOnly,
        /// <summary>control is dead, that means it was removed from the logical or visual tree.</summary>
        Dead,
    }
}
