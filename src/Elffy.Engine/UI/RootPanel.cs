#nullable enable
using Cysharp.Threading.Tasks;
using Elffy.Shading;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Elffy.UI
{
    /// <summary>Root panel of UI tree</summary>
    public sealed class RootPanel : Panel
    {
        private readonly UIObjectLayer _uiLayer;
        private Control? _relayoutRoot;
        private LayoutExecutionType _layoutExecutionType;
        private bool _relayoutRequested;

        internal UIObjectLayer UILayer => _uiLayer;

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

        internal RootPanel(UIObjectLayer uiLayer)
        {
            Debug.Assert(uiLayer is not null);
            _uiLayer = uiLayer;
            _relayoutRequested = true;
        }

        internal async UniTask Initialize(FrameTimingPoint timingPoint, CancellationToken ct)
        {
            var layer = _uiLayer;
            Debug.Assert(layer.LifeState >= LifeState.Alive);
            var screen = layer.Screen;
            Debug.Assert(screen is not null);
            SetAsRootControl();
            await Renderable.ActivateOnLayer(layer, timingPoint, ct);
            Renderable.BeforeRendering += ExecuteRelayout;
        }

        private void ExecuteRelayout(in RenderingContext context)
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
                OnLayoutChildreRecursively(ControlLayoutContext.Default);
            }
        }

        public void RequestRelayout()
        {
            _relayoutRequested = true;
        }
    }
}
