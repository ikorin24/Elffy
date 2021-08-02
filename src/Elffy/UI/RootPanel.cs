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
        private readonly UILayer _uiLayer;
        private LayoutExecutionType _layoutExecutionType;

        internal UILayer UILayer => _uiLayer;

        /// <summary>Get or set layout execution type.</summary>
        public LayoutExecutionType LayoutExecutionType
        {
            get => _layoutExecutionType;
            set => _layoutExecutionType = value;
        }

        /// <summary><see cref="RootPanel"/> doesn't support it. It throws <see cref="InvalidOperationException"/>.</summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("RootPanel does not support 'Margin'.", true)]
        public new ref LayoutThickness Margin => throw new InvalidOperationException($"{nameof(RootPanel)} does not support '{nameof(Margin)}'.");

        internal RootPanel(UILayer uiLayer)
        {
            if(uiLayer is null) {
                ThrowNullArg();
                [DoesNotReturn] static void ThrowNullArg() => throw new ArgumentNullException(nameof(uiLayer));
            }
            _uiLayer = uiLayer;
        }

        internal void Initialize()
        {
            SetAsRootControl();
            Renderable.ActivateOnInternalLayer(UILayer);
            Renderable.BeforeRendering += ExecuteRelayout;
        }

        private void ExecuteRelayout(Renderable sender, in Matrix4 model, in Matrix4 view, in Matrix4 projection)
        {
            switch(_layoutExecutionType) {
                case LayoutExecutionType.Explicit:
                    return;
                case LayoutExecutionType.EveryFrame:
                    LayoutChildren();
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary><see cref="RootPanel"/> doesn't support it. It throws <see cref="InvalidOperationException"/>.</summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("RootPanel does not support 'LayoutSelf'.", true)]
        public new void LayoutSelf() => throw new InvalidOperationException($"{nameof(RootPanel)} does not support '{nameof(LayoutSelf)}'.");

        /// <summary><see cref="RootPanel"/> doesn't support it. It throws <see cref="InvalidOperationException"/>.</summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("RootPanel does not support 'LayoutSelf'.", true)]
        public new void LayoutSelf<T>(ControlLayoutResolver<T> resolver, T state) => throw new InvalidOperationException($"{nameof(RootPanel)} does not support '{nameof(LayoutSelf)}'.");
    }
}
