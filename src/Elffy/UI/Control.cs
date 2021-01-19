#nullable enable
using System;
using System.Drawing;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Elffy.InputSystem;
using Elffy.Components;
using Elffy.Core;
using Elffy.Shading;
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
        private readonly UIRenderable _renderable;
        private ControlLayouterInternal? _layouter;
        private Control? _parent;
        private RootPanel? _root;
        private Vector2i _absolutePosition;
        private Vector2i _offset;
        private ArrayPooledListCore<Control> _childrenCore;
        private TextureCore _texture;
        private Color4 _background;
        private bool _isHitTestVisible;
        private bool _isMouseOver;

        internal ref ArrayPooledListCore<Control> ChildrenCore => ref _childrenCore;

        internal UIRenderable Renderable => _renderable;

        /// <summary>Get life state</summary>
        public LifeState LifeState => Renderable.LifeState;

        public ControlCollection Children => new(this);

        public Control? Parent => _parent;

        public RootPanel? Root => _root;

        public bool IsRoot => _parent is null;

        public ControlLayouter Layout
        {
            get
            {
                return _layouter is not null ? new ControlLayouter(_layouter) : Throw();

                static ControlLayouter Throw() => throw new InvalidOperationException($"Cannot get {nameof(ControlLayouter)}.");
            }
        }

        public UIShaderSource? Shader
        {
            get => SafeCast.As<UIShaderSource>(_renderable.ShaderInternal);
            set => _renderable.ShaderInternal = value;
        }

        public ref Color4 Background => ref _background;

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
                    if(_childrenCore.Count > 0) {
                        ApplyRecursively(this, in vec);
                    }
                }
                else if(vec.X != 0) {
                    // change only x
                    pos.X += vec.X;
                    _absolutePosition.X += vec.X;
                    if(_childrenCore.Count > 0) {
                        ApplyXRecursively(this, vec.X);
                    }
                }
                else if(vec.Y != 0) {
                    // change only y
                    pos.Y += vec.Y;
                    _absolutePosition.Y += vec.Y;
                    if(_childrenCore.Count > 0) {
                        ApplyYRecursively(this, vec.Y);
                    }
                }

                [MethodImpl(MethodImplOptions.NoInlining)]  // no inlining
                static void ApplyRecursively(Control parent, in Vector2i vec)
                {
                    foreach(var child in parent._childrenCore.AsSpan()) {
                        child._absolutePosition += vec;
                        ApplyRecursively(child, vec);
                    }
                }

                [MethodImpl(MethodImplOptions.NoInlining)]  // no inlining
                static void ApplyXRecursively(Control parent, int diff)
                {
                    foreach(var child in parent._childrenCore.AsSpan()) {
                        child._absolutePosition.X += diff;
                        ApplyXRecursively(child, diff);
                    }
                }

                [MethodImpl(MethodImplOptions.NoInlining)]  // no inlining
                static void ApplyYRecursively(Control parent, int diff)
                {
                    foreach(var child in parent._childrenCore.AsSpan()) {
                        child._absolutePosition.Y += diff;
                        ApplyYRecursively(child, diff);
                    }
                }
            }
        }

        public ref readonly Vector2i AbsolutePosition => ref _absolutePosition;

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
        public Vector2i Size
        {
            get => (Vector2i)Renderable.Scale.Xy;
            set
            {
                if(value.X < 0 || value.Y < 0) {
                    ThrowOutOfRange();
                    static void ThrowOutOfRange() => throw new ArgumentOutOfRangeException();
                }
                Renderable.Scale.X = value.X;
                Renderable.Scale.Y = value.Y;
            }
        }

        /// <summary>get or set offset position of layout</summary>
        public ref Vector2i Offset => ref _offset;

        /// <summary>get or set whether this <see cref="Control"/> is enabled in HitTest</summary>
        public bool IsHitTestVisible
        {
            get => _isHitTestVisible;
            set => _isHitTestVisible = value;
        }

        /// <summary>get whether the mouse is over this <see cref="Control"/></summary>
        public bool IsMouseOver => _isMouseOver;

        /// <summary>get or set <see cref="Control"/> is visible on rendering.</summary>
        public bool IsVisible { get => Renderable.IsVisible; set => Renderable.IsVisible = value; }

        internal ref readonly TextureCore Texture => ref _texture;

        /// <summary>Mouse enter event</summary>
        public event Action<Control, MouseEventArgs>? MouseEnter;
        /// <summary>Mouse leave event</summary>
        public event Action<Control, MouseEventArgs>? MouseLeave;

        public event Action<Control>? Activated;
        public event Action<Control>? Dead;

        /// <summary>constructor of <see cref="Control"/></summary>
        public Control()
        {
            _isHitTestVisible = true;
            _layouter = ControlLayouterInternal.Create();
            _renderable = new UIRenderable(this);
            Width = 30;
            Height = 30;
            _texture = new TextureCore(TextureExpansionMode.Bilinear, TextureShrinkMode.Bilinear, TextureMipmapMode.None, TextureWrapMode.ClampToEdge, TextureWrapMode.ClampToEdge);
            Renderable.Activated += OnRenderableActivated;
            Renderable.Dead += OnRenderableDead;
        }

        protected private void SetAsRootControl()
        {
            Debug.Assert(this is RootPanel);
            _root = SafeCast.As<RootPanel>(this);
        }

        protected virtual void OnRecieveHitTestResult(bool isHit, Mouse mouse)
        {
            var isMouseOverPrev = IsMouseOver;
            _isMouseOver = isHit;
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
            if(_texture.IsEmpty) {
                _texture.LoadUndefined(new Vector2i(Width, Height));
                var p = _texture.GetPainter(false);
                if(copyFromOriginal) {
                    p.Fill(ColorByte.White);        // TODO: デフォルトが白じゃない場合は？
                }
                return p;
            }
            else {
                return _texture.GetPainter(copyFromOriginal);
            }
        }

        public TexturePainter GetPainter(in RectI rect, bool copyFromOriginal = true)
        {
            if(_texture.IsEmpty) {
                _texture.LoadUndefined(new Vector2i(Width, Height));
                var p = _texture.GetPainter(rect, copyFromOriginal);
                p.Fill(ColorByte.White);            // TODO: デフォルトが白じゃない場合は？
                return p;
            }
            else {
                return _texture.GetPainter(rect, copyFromOriginal);
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
            _root = parent.Root;
            Renderable.Activate(_root.UILayer);
        }

        internal void RemovedFromListCallback()
        {
            _parent = null;
            _root = null;
            Renderable.Terminate();
        }

        private void OnRenderableActivated(FrameObject sender)
        {
            Activated?.Invoke(this);
        }

        private void OnRenderableDead(FrameObject sender)
        {
            Dead?.Invoke(this);
            Debug.Assert(_layouter is not null);
            ControlLayouterInternal.Return(ref _layouter);
        }
    }
}
