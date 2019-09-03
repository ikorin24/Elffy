using Elffy.Core;
using OpenTK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elffy.UI
{
    /// <summary>UI の Root となるオブジェクト</summary>
    public class Page : Renderable
    {
        /// <summary>この <see cref="Page"/> が レイアウト済みかどうかを取得します</summary>
        public bool IsLayouted { get; private set; }

        #region Layout
        /// <summary>Layout を実行します</summary>
        public void Layout()
        {
            if(IsLayouted) { throw new ArgumentException($"Already layouted."); }

            // UIBase 以外の要素が UITree に含まれる場合は InvalidCastException
            // 深さ優先探索で列挙されるため子より親が先に Position が決定する
            try {
                var design = GetOffspring().Cast<UIBase>().Select(u => new LayoutDesign(u, (UIBase)u.Parent, u.Position));
                foreach(var item in design) {
                    var target = item.Target;
                    var parent = item.Parent;
                    Debug.Assert(target.Parent != null);

                    // 座標が NaN ならレイアウトを計算する。
                    var posXCalcRequirement = float.IsNaN(item.InitialPosition.X);
                    var posYCalcRequirement = float.IsNaN(item.InitialPosition.Y);
                    target.PositionZ = float.IsNaN(item.InitialPosition.Z) ? 0f : item.InitialPosition.Z;
                    if(posXCalcRequirement) {
                        switch(target.HorizontalAlignment) {
                            case HorizontalAlignment.Left:
                                target.PositionX = target.OffsetX;
                                break;
                            case HorizontalAlignment.Center:
                                target.PositionX = (parent.Width - target.Width) / 2  + target.OffsetX;
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
                                target.PositionY = (parent.Height - target.Height) / 2 -  target.OffsetY;
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
            catch(InvalidCastException ex) {
                throw new InvalidOperationException($"This {nameof(Page)} includes non {nameof(UIBase)} element.", ex);
            }
        }
        #endregion

        #region class LayoutDesign
        class LayoutDesign
        {
            public UIBase Target { get; }
            public UIBase Parent { get; }
            public Vector3 InitialPosition { get; }
            public LayoutDesign(UIBase target, UIBase parent, Vector3 initialPos)
            {
                Target = target;
                InitialPosition = initialPos;
            }
        }
        #endregion
    }
}
