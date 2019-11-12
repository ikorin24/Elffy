using Elffy.Core;
using Elffy.Exceptions;
using OpenTK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elffy.UI
{
    /// <summary>UI tree の Root となるオブジェクト</summary>
    internal class Page : Panel, IUIRoot
    {
        /// <summary>この <see cref="Page"/> が レイアウト済みかどうかを取得します</summary>
        public bool IsLayouted { get; private set; }

        /// <summary><see cref="IUIRoot"/> 実装。この <see cref="Page"/> とその子孫を描画する <see cref="Layer"/></summary>
        public Layer UILayer { get; }

        /// <summary>コンストラクタ</summary>
        /// <param name="uiLayer">この <see cref="Page"/> と子孫を描画する UI レイヤー</param>
        internal Page(Layer uiLayer)
        {
            ArgumentChecker.ThrowIfNullArg(uiLayer, nameof(uiLayer));
            UILayer = uiLayer;
            Root = this;
        }

        #region Layout
        /// <summary>Layout を実行します</summary>
        public void Layout()
        {
            if(IsLayouted) { throw new InvalidOperationException($"Already layouted."); }

            // 深さ優先探索で列挙されるため子より親が先に Position が決定する
            foreach(var target in GetOffspring()) {
                Control parent = target.Parent;
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
        #endregion
    }
}
