#nullable enable
using Elffy.Core;
using System;
using System.Diagnostics.CodeAnalysis;
using System.ComponentModel;

namespace Elffy.UI
{
    /// <summary>Root panel of UI tree</summary>
    public sealed class RootPanel : Panel
    {
        internal UILayer UILayer { get; }

        /// <summary>Don't call the property. <see cref="RootPanel"/> doesn't support it. It throws <see cref="InvalidOperationException"/>.</summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("RootPanel does not support 'Padding'.", true)]
        public new ref LayoutThickness Padding => throw new InvalidOperationException($"{nameof(RootPanel)} does not support '{nameof(Padding)}'.");

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

        /// <summary>Don't call the method. <see cref="RootPanel"/> doesn't support it. It throws <see cref="InvalidOperationException"/>.</summary>
        [EditorBrowsable(EditorBrowsableState.Never)]   // Hide the method. (RootPanel do nothing even if the method is called.)
        [Obsolete("RootPanel does not support 'LayoutSelf'.", true)]
        public new void LayoutSelf() => throw new InvalidOperationException($"{nameof(RootPanel)} does not support '{nameof(LayoutSelf)}'.");
    }
}
