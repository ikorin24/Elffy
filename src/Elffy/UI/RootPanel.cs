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
            Renderable.Shader = new UISolidColorShaderSource(Color4.Transparent);
            Renderable.Activate(UILayer);
        }

#if false   // future version, auto layout
        public bool IsLayouted { get; private set; }

        /// <summary>Layout を実行します</summary>
        public void Layout()
        {
            if(IsLayouted) { throw new InvalidOperationException($"Already layouted."); }

            // 深さ優先探索で列挙されるため子より親が先に Position が決定する
            foreach(var target in GetOffspring()) {
                Control parent = target.Parent!;
                Debug.Assert(target.Parent != null);

                // 座標が NaN ならレイアウトを計算する。
                var posXCalcRequirement = float.IsNaN(target.PositionX);
                var posYCalcRequirement = float.IsNaN(target.PositionY);
                if(posXCalcRequirement) {
                    switch(target.HorizontalAlignment) {
                        case HorizontalAlignment.Left:
                            target.PositionX = target.OffsetX;
                            break;
                        case HorizontalAlignment.Center:
                            target.PositionX = (parent.Width - target.Width) / 2 + target.OffsetX;
                            break;
                        case HorizontalAlignment.Right:
                            target.PositionX = parent.Width - target.Width + target.OffsetX;
                            break;
                        default:
                            throw new NotSupportedException($"Unknown type of {nameof(HorizontalAlignment)} : {target.HorizontalAlignment}");
                    }
                }
                if(posYCalcRequirement) {
                    switch(target.VerticalAlignment) {
                        case VerticalAlignment.Top:
                            target.PositionY = target.OffsetY;
                            break;
                        case VerticalAlignment.Center:
                            target.PositionY = (parent.Height - target.Height) / 2 - target.OffsetY;
                            break;
                        case VerticalAlignment.Bottom:
                            target.PositionY = parent.Height - target.Height - target.OffsetY;
                            break;
                        default:
                            throw new NotSupportedException($"Unknown type of {nameof(VerticalAlignment)} : {target.VerticalAlignment}");
                    }
                }
            }
            IsLayouted = true;
        }
#endif      // future version, auto layout
    }
}
