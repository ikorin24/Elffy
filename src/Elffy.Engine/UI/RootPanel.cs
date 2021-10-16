#nullable enable
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Elffy.UI
{
    /// <summary>Root panel of UI tree</summary>
    public sealed class RootPanel : Panel
    {
        private readonly UILayer _uiLayer;
        private Control? _relayoutRoot;
        private LayoutExecutionType _layoutExecutionType;
        private bool _relayoutRequested;

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
            Debug.Assert(uiLayer is not null);
            _uiLayer = uiLayer;
            _relayoutRequested = true;
        }

        internal void Initialize()
        {
            SetAsRootControl();
            Renderable.ActivateOnUILayer(UILayer);
            Renderable.BeforeRendering += ExecuteRelayout;
        }

        private void ExecuteRelayout(Renderable sender, in Matrix4 model, in Matrix4 view, in Matrix4 projection)
        {
            var type = _layoutExecutionType;
            if(type == LayoutExecutionType.Adaptive) {
                Relayout(false);
            }
            else if(type == LayoutExecutionType.EveryFrame) {
                Relayout(true);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Relayout(bool forceToRelayout = false)
        {
            if(_relayoutRequested || forceToRelayout) {
                _relayoutRequested = false;
                ControlLayoutHelper.LayoutChildrenRecursively(this);
            }
        }

        public void RequestRelayout()
        {
            _relayoutRequested = true;
        }
    }
}
