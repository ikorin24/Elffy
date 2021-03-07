#nullable enable
using Elffy.Core;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Elffy.UI
{
    /// <summary>Root panel of UI tree</summary>
    public sealed class RootPanel : Panel
    {
        internal UILayer UILayer { get; }

        internal RootPanel(UILayer uiLayer)
        {
            if(uiLayer is null) {
                ThrowNullArg();
                [DoesNotReturn] static void ThrowNullArg() => throw new ArgumentNullException(nameof(uiLayer));
            }
            UILayer = uiLayer;
        }

        internal void Initialize()
        {
            SetAsRootControl();
            Renderable.Activate(UILayer);
        }

        public void LayoutUI()
        {
            // RootPanel.Layout is ignored.
            LayoutChildren();
        }
    }
}
