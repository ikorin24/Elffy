#nullable enable
using Elffy.Core;
using Elffy.Shading;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Elffy.UI
{
    /// <summary>UI tree の Root となるオブジェクト</summary>
    public sealed class RootPanel : Panel
    {
        /// <summary>この <see cref="RootPanel"/> とその子孫を描画するレイヤー</summary>
        internal UILayer UILayer { get; }

        /// <summary>コンストラクタ</summary>
        /// <param name="uiLayer">この <see cref="RootPanel"/> と子孫を描画する UI レイヤー</param>
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

            var parentSize = (Vector2)Size;
            var parentPadding = new LayoutThickness();

            foreach(var child in Children.AsSpan()) {
                child.Layout(parentSize, parentPadding);
            }
        }
    }
}
