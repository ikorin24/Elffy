#nullable enable
using System;
using System.Drawing;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Elffy.InputSystem;
using Elffy.Components;
using Elffy.Core;
using System.Diagnostics;

namespace Elffy.UI
{
    // I don't support forcus of control.

    /// <summary>
    /// UI の要素の基底クラス。UI への配置, ヒットテストの処理を提供します<para/>
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

        /// <summary>Get life state</summary>
        public LifeState LifeState => Renderable.LifeState;

        /// <summary>この <see cref="Control"/> のツリー構造の子要素を取得します</summary>
        public ControlCollection Children { get; }

        /// <summary>この <see cref="Control"/> のツリー構造の親を取得します</summary>
        public Control? Parent => _parent;
        private Control? _parent;

        /// <summary>この <see cref="Control"/> を持つ UI tree の Root</summary>
        public RootPanel? Root { get; protected private set; }

        public Vector2i Position
        {
            get => (Vector2i)Renderable.Position.Xy;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                ref var pos = ref Renderable.Position;
                var vec = value - (Vector2i)pos.Xy;

                if(vec.X != 0 && vec.Y != 0) {
                    // change both x and y

                    pos.X += vec.X;
                    pos.Y += vec.Y;
                    _absolutePosition += vec;
                    if(Children.Count > 0) {
                        ApplyRecursively(Children, in vec);
                    }
                }
                else if(vec.X != 0) {
                    // change only x
                    pos.X += vec.X;
                    _absolutePosition.X += vec.X;
                    if(Children.Count > 0) {
                        ApplyXRecursively(Children, vec.X);
                    }
                }
                else if(vec.Y != 0) {
                    // change only y
                    pos.Y += vec.Y;
                    _absolutePosition.Y += vec.Y;
                    if(Children.Count > 0) {
                        ApplyYRecursively(Children, vec.Y);
                    }
                }

                [MethodImpl(MethodImplOptions.NoInlining)]  // no inlining
                static void ApplyRecursively(ControlCollection children, in Vector2i vec)
                {
                    foreach(var child in children.AsReadOnlySpan()) {
                        child._absolutePosition += vec;
                        ApplyRecursively(child.Children, vec);
                    }
                }

                [MethodImpl(MethodImplOptions.NoInlining)]  // no inlining
                static void ApplyXRecursively(ControlCollection children, int diff)
                {
                    foreach(var child in children.AsReadOnlySpan()) {
                        child._absolutePosition.X += diff;
                        ApplyXRecursively(child.Children, diff);
                    }
                }

                [MethodImpl(MethodImplOptions.NoInlining)]  // no inlining
                static void ApplyYRecursively(ControlCollection children, int diff)
                {
                    foreach(var child in children.AsReadOnlySpan()) {
                        child._absolutePosition.Y += diff;
                        ApplyYRecursively(child.Children, diff);
                    }
                }
            }
        }

        public ref readonly Vector2i AbsolutePosition => ref _absolutePosition;
        private Vector2i _absolutePosition;

        /// <summary>get or set Width of <see cref="Control"/></summary>
        public int Width
        {
            get => (int)Renderable.Scale.X;
            set
            {
                if(value < 0) {
                    ThrowOutOfRange();
                    static void ThrowOutOfRange() => throw new ArgumentOutOfRangeException();
                }
                Renderable.Scale.X = value;
            }
        }


        /// <summary>get or set Height of <see cref="Control"/></summary>
        public int Height
        {
            get => (int)Renderable.Scale.Y;
            set
            {
                if(value < 0) {
                    ThrowOutOfRange();
                    static void ThrowOutOfRange() => throw new ArgumentOutOfRangeException();
                }
                Renderable.Scale.Y = value;
            }
        }

        /// <summary>get or set size of <see cref="Control"/></summary>
        public Vector2i Size { get => new(Width, Height); set => (Width, Height) = value; }

        /// <summary>get or set offset position X of layout</summary>
        public int OffsetX { get; set; }
        /// <summary>get or set offset position Y of layout</summary>
        public int OffsetY { get; set; }

        /// <summary>get or set offset position of layout</summary>
        public Vector2i Offset { get => new(OffsetX, OffsetY); set => (OffsetX, OffsetY) = value; }

#if false   // future version, auto layout

        /// <summary>get or set horizontal alignment of layout</summary>
        public HorizontalAlignment HorizontalAlignment { get; set; }
        /// <summary>get or set vertical alignment of layout</summary>
        public VerticalAlignment VerticalAlignment { get; set; }

#endif      // future version, auto layout

        /// <summary>get or set whether this <see cref="Control"/> is enable in HitTest</summary>
        public bool IsHitTestVisible { get; set; } = true;
        /// <summary>get whether the mouse is over this <see cref="Control"/></summary>
        public bool IsMouseOver { get; private set; }

        /// <summary>get or set <see cref="Control"/> is visible on rendering.</summary>
        public bool IsVisible { get => Renderable.IsVisible; set => Renderable.IsVisible = value; }

        internal Texture Texture { get; }

        /// <summary>Mouse enter event</summary>
        public event Action<Control, MouseEventArgs>? MouseEnter;
        /// <summary>Mouse leave event</summary>
        public event Action<Control, MouseEventArgs>? MouseLeave;

        public event Action<Control>? Alive;
        public event Action<Control>? Dead;

        /// <summary>constructor of <see cref="Control"/></summary>
        public Control()
        {
            Children = new ControlCollection(this);
            Renderable = new UIRenderable(this);
            Width = 30;
            Height = 30;
            Texture = new Texture(TextureExpansionMode.Bilinear, TextureShrinkMode.Bilinear, TextureMipmapMode.None, TextureWrapMode.ClampToEdge, TextureWrapMode.ClampToEdge);
            Renderable.Alive += OnRenderableAlive;
            Renderable.Dead += OnRenderableDead;
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

        public TexturePainter GetPainter(bool copyFromOriginal = true)
        {
            var texture = Texture;
            if(texture.IsEmpty) {
                texture.LoadUndefined(new Vector2i(Width, Height));
                var p = texture.GetPainter(false);
                if(copyFromOriginal) {
                    p.Fill(ColorByte.White);
                }
                return p;
            }
            else {
                return texture.GetPainter(copyFromOriginal);
            }
        }

        public TexturePainter GetPainter(in RectI rect, bool copyFromOriginal = true)
        {
            var texture = Texture;
            if(texture.IsEmpty) {
                texture.LoadUndefined(new Vector2i(Width, Height));
                var p = texture.GetPainter(rect, copyFromOriginal);
                p.Fill(ColorByte.White);
                return p;
            }
            else {
                return texture.GetPainter(rect, copyFromOriginal);
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
                   new Rectangle(_absolutePosition.X, _absolutePosition.Y, Width, Height)
                        .Contains((int)mouse.Position.X, (int)mouse.Position.Y);
        }

        /// <summary>ヒットテストの結果を通知します</summary>
        /// <param name="isHit">ヒットテスト結果</param>
        /// <param name="mouse">マウス</param>
        internal void NotifyHitTestResult(bool isHit, Mouse mouse)
        {
            OnRecieveHitTestResult(isHit, mouse);
        }

        internal void AddedToListCallback(Control parent)
        {
            Debug.Assert(parent is not null);
            Debug.Assert(parent.Root is not null);
            _parent = parent;
            Root = parent.Root;
            Renderable.Activate(Root.UILayer);
        }

        internal void RemovedFromListCallback()
        {
            _parent = null;
            Root = null;
            Renderable.Terminate();
        }

        private void OnRenderableAlive(FrameObject sender)
        {
            // Attached component is disposed automatically when Renderable dies.
            SafeCast.As<ComponentOwner>(sender).AddComponent(Texture);
            Alive?.Invoke(this);
        }

        private void OnRenderableDead(FrameObject sender)
        {
            Dead?.Invoke(this);
        }
    }

#if false   // future version, auto layout

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

#endif      // future version, auto layout
}
