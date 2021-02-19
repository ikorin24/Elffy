#nullable enable
using System;
using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Elffy.UI
{
    public static class VisualTree
    {
        public static ControlVisualChildren GetChildren(Control control)
        {
            if(control is null) { ThrowNullArg(nameof(control)); }

            if(control.LifeState != ControlLifeState.InLogicalTree) {
                return ControlVisualChildren.Empty;
            }
            return new ControlVisualChildren(control);
        }

        public static int GetChildrenCount(Control control)
        {
            if(control is null) { ThrowNullArg(nameof(control)); }
            var last = control.LastVisualChild;
            return last is null ? 0 : last.VisualIndex + 1;
        }

        public static Control? GetParent(Control control)
        {
            if(control is null) { ThrowNullArg(nameof(control)); }
            if(control.IsRoot) {
                Debug.Assert(control is RootPanel);
                return null;
            }
            return control.Parent;  // Parent in visual tree is same as logical parent.
        }

        [DoesNotReturn] private static void ThrowNullArg(string name) => throw new ArgumentNullException(name);
    }
}
