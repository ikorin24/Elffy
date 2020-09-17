#nullable enable
using System;
using System.Drawing;
using System.Collections.Generic;
using System.Diagnostics;
using Elffy.InputSystem;

namespace Elffy.UI
{
    /// <summary>
    /// UI の要素の基底クラス。UI への配置, フォーカス処理, ヒットテストの処理を提供します<para/>
    /// </summary>
    /// 
    /// <remarks>
    /// <see cref="Control"/> はUIを構成する論理的なコントロールとしての機能のみを提供します。
    /// 画面への描画に関する処理は、このクラスと対になる <see cref="Core.Renderable"/> 継承クラス <see cref="UIRenderable"/> のインスタンスに任されます。
    /// <see cref="UIRenderable"/> による描画は <see cref="Control"/> によって完全に隠蔽され、外部からは描画を気にすることなく論理的 UI 構造を扱えます。
    /// 
    /// <see cref="Control"/> の木構造は、描画を担当する <see cref="Core.Renderable"/> オブジェクトの木構造とは独立しています。
    /// <see cref="Control"/> が親子関係の木構造を形成している場合でも、その描画オブジェクトは木構造を作らず、
    /// 常に <see cref="UIRenderable"/> は Root に存在します。
    /// </remarks>
    public abstract class Control
    {
        /// <summary>この <see cref="Control"/> を描画するオブジェクト</summary>
        internal UIRenderable Renderable { get; private set; }

        /// <summary>この <see cref="Control"/> のツリー構造の子要素を取得します</summary>
        public ControlCollection Children { get; }

        /// <summary>この <see cref="Control"/> のツリー構造の親を取得します</summary>
        public Control? Parent
        {
            get => _parent;
            internal set
            {
                if(_parent == null) {
                    _parent = value;
                    Root = value?.Root;
                }
                else if(_parent != null && value == null) {
                    _parent = null;
                    Root = null;
                }
                else { ThrowInvalidOperation(); }

                static void ThrowInvalidOperation() => throw new InvalidOperationException($"The instance is already a child of another object. Can not has multi parents.");
            }
        }
        private Control? _parent;

        /// <summary>この <see cref="Control"/> を持つ UI tree の Root</summary>
        public Page? Root { get; protected private set; }

        public Vector2i Position
        {
            get => (Vector2i)Renderable.Position.Xy;
            set
            {
                var vec = value - Position;
                Renderable.Position += new Vector3(vec.X, vec.Y, 0);
                _absolutePosition += vec;

                // Optimization for inlining in case of `this` has no children
                // by making loop a function.
                if(Children.Count > 0) {
                    ApplyRecursively(Children, in vec);
                }

                static void ApplyRecursively(ControlCollection children, in Vector2i vec)
                {
                    foreach(var child in children.AsReadOnlySpan()) {
                        child._absolutePosition += vec;
                        ApplyRecursively(child.Children, vec);
                    }
                }
            }
        }

        public int PositionX
        {
            get => (int)Renderable.Position.X;
            set
            {
                var diff = value - PositionX;
                Renderable.Position.X += diff;
                _absolutePosition.X += diff;

                // Optimization for inlining in case of `this` has no children
                // by making loop a function.
                if(Children.Count > 0) {
                    ApplyRecursively(Children, diff);
                }

                static void ApplyRecursively(ControlCollection children, int diff)
                {
                    foreach(var child in children.AsReadOnlySpan()) {
                        child._absolutePosition.X += diff;
                        ApplyRecursively(child.Children, diff);
                    }
                }
            }
        }

        /// <summary>オブジェクトのY座標</summary>
        public int PositionY
        {
            get => (int)Renderable.Position.Y;
            set
            {
                var diff = value - PositionY;
                Renderable.Position.Y += diff;
                _absolutePosition.Y += diff;

                // Optimization for inlining in case of `this` has no children
                // by making loop a function.
                if(Children.Count > 0) {
                    ApplyRecursively(Children, diff);
                }

                static void ApplyRecursively(ControlCollection children, int diff)
                {
                    foreach(var child in children.AsReadOnlySpan()) {
                        child._absolutePosition.Y += diff;
                        ApplyRecursively(child.Children, diff);
                    }
                }
            }
        }

        public ref readonly Vector2i AbsolutePosition => ref _absolutePosition;
        private Vector2i _absolutePosition;

        /// <summary>get or set Width of <see cref="Control"/></summary>
        public int Width { get => _width; set => _width = Math.Max(0, value); }
        private int _width;

        /// <summary>get or set Height of <see cref="Control"/></summary>
        public int Height { get => _height; set => _height = Math.Max(0, value); }
        private int _height;

        /// <summary>get or set offset position X of layout</summary>
        public int OffsetX { get; set; }
        /// <summary>get or set offset position Y of layout</summary>
        public int OffsetY { get; set; }
        /// <summary>get or set horizontal alignment of layout</summary>
        public HorizontalAlignment HorizontalAlignment { get; set; }
        /// <summary>get or set vertical alignment of layout</summary>
        public VerticalAlignment VerticalAlignment { get; set; }
        /// <summary>get or set whether this <see cref="Control"/> can be focused</summary>
        public bool IsFocusable { get; set; }

        /// <summary>get whether this <see cref="Control"/> is focused</summary>
        public bool IsFocused
        {
            get => _isFocused;
            internal set
            {
                if(_isFocused == value) { return; }
                _isFocused = value;
                if(_isFocused) {
                    Debug.Assert(IsFocusable);
                    FocusEnter?.Invoke(this);
                }
                else {
                    FocusLost?.Invoke(this);
                }
            }
        }
        private bool _isFocused;

        /// <summary>get or set whether this <see cref="Control"/> is enable in HitTest</summary>
        public bool IsHitTestVisible { get; set; } = true;
        /// <summary>get whether the mouse is over this <see cref="Control"/></summary>
        public bool IsMouseOver { get; private set; }

        /// <summary>get or set <see cref="Control"/> is visible on rendering.</summary>
        public bool IsVisible { get => Renderable.IsVisible; set => Renderable.IsVisible = value; }

        ///// <summary>Get or set texture</summary>
        ///// <exception cref="ArgumentNullException"></exception>
        //public TextureBase Texture { get => Renderable.Texture; set => Renderable.Texture = value; }

        /// <summary>Focus enter event</summary>
        public event ActionEventHandler<Control>? FocusEnter;
        /// <summary>Focus lost event</summary>
        public event ActionEventHandler<Control>? FocusLost;
        /// <summary>Mouse enter event</summary>
        public event ActionEventHandler<Control, MouseEventArgs>? MouseEnter;
        /// <summary>Mouse leave event</summary>
        public event ActionEventHandler<Control, MouseEventArgs>? MouseLeave;

        /// <summary>constructor of <see cref="Control"/></summary>
        public Control()
        {
            Children = new ControlCollection(this);
            Renderable = new UIRenderable(this);
        }

        protected virtual void OnRecieveHitTestResult(bool isHit, Mouse mouse)
        {
            var isMouseOverPrev = IsMouseOver;
            IsMouseOver = isHit;
            if(isHit) {
                if(isMouseOverPrev == false) {
                    MouseEnter?.Invoke(this, new MouseEventArgs(mouse.Position));
                }
            }
            else {
                if(isMouseOverPrev) {
                    MouseLeave?.Invoke(this, new MouseEventArgs(mouse.Position));
                }
            }
        }

        /// <summary>このオブジェクトの <see cref="Children"/> 以下に存在する全ての子孫を取得します。列挙順は深さ優先探索 (DFS; depth-first search) です。</summary>
        /// <returns>全ての子孫オブジェクト</returns>
        public IEnumerable<Control> GetOffspring()
        {
            foreach(var child in Children) {
                yield return child;
                foreach(var offspring in child.GetOffspring()) {
                    yield return offspring;
                }
            }
        }

        /// <summary>マウスオーバーしているかを取得します</summary>
        /// <param name="mouse">マウス</param>
        /// <returns>マウスオーバーしているか</returns>
        internal bool MouseOverTest(Mouse mouse)
        {
            return IsVisible &&
                   IsHitTestVisible &&
                   new Rectangle(_absolutePosition.X, _absolutePosition.Y, _width, _height)
                        .Contains((int)mouse.Position.X, (int)mouse.Position.Y);
        }

        /// <summary>ヒットテストの結果を通知します</summary>
        /// <param name="isHit">ヒットテスト結果</param>
        /// <param name="mouse">マウス</param>
        internal void NotifyHitTestResult(bool isHit, Mouse mouse)
        {
            OnRecieveHitTestResult(isHit, mouse);
        }
    }

    #region enum HorizontalAlignment
    /// <summary>Layout horizontal alignment</summary>
    public enum HorizontalAlignment
    {
        /// <summary>left alignment</summary>
        Left,
        /// <summary>center alignment</summary>
        Center,
        /// <summary>right alignment</summary>
        Right,
    }
    #endregion

    #region enum VerticalAlignment
    /// <summary>Layout vertical alignment</summary>
    public enum VerticalAlignment
    {
        /// <summary>top alignment</summary>
        Top,
        /// <summary>center alignment</summary>
        Center,
        /// <summary>bottom alignment</summary>
        Bottom,
    }
    #endregion

    #region class MouseEventArgs
    /// <summary>Mouse event argument class</summary>
    public readonly struct MouseEventArgs
    {
        /// <summary>mouse position</summary>
        public Vector2 MousePosition { get; }

        /// <summary>constructor</summary>
        /// <param name="mousePosition">mouse position</param>
        public MouseEventArgs(in Vector2 mousePosition)
        {
            MousePosition = mousePosition;
        }
    }
    #endregion
}
