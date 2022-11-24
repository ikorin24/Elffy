#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using Elffy.Features.Internal;
using System.Collections;
using System.ComponentModel;

namespace Elffy
{
    /// <summary>Base class which exists in space. That provides position, size and rotation.</summary>
    public abstract class Positionable : ComponentOwner
    {
        private Trs<Positionable> _trs = new Trs<Positionable>();           // mutable object, don't make it readonly
        private ArrayPooledListCore<Positionable> _childrenCore = new();    // mutable object, don't make it readonly
        private EventSource<Positionable> _parentChanged;                   // mutable object, don't make it readonly
        private Matrix4? _modelCache;
        private Positionable? _parent;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        internal ref ArrayPooledListCore<Positionable> ChildrenCore => ref _childrenCore;   // don't make it ref readonly

        /// <summary>Get or set <see cref="Quaternion"/> of rotation.</summary>
        public Quaternion Rotation
        {
            get => _trs.Rotation;
            set
            {
                if(_trs.SetRotation(value, out var changed)) {
                    changed.InvokeIgnoreException(this);
                }
            }
        }

        /// <summary>Get parent of the <see cref="Positionable"/>.</summary>
        public Positionable? Parent
        {
            get => _parent;
            internal set
            {
                Debug.Assert((_parent is null) ^ (value is null), "Either the current parent or the new one should be null.");
                _parent = value;

                _modelCache = null;
                _parentChanged.InvokeIgnoreException(this);
            }
        }

        /// <summary>Get children of the <see cref="Positionable"/></summary>
        public PositionableCollection Children => new(this);

        /// <summary>Get whether the <see cref="Positionable"/> is a root object in the tree.</summary>
        public bool IsRoot => _parent is null;

        /// <summary>Get whether the <see cref="Positionable"/> has any children.</summary>
        public bool HasChild => _childrenCore.Count > 0;

        /// <summary>Get or set local position whose origin is the position of <see cref="Parent"/>.</summary>
        /// <remarks>The value is same as <see cref="WorldPosition"/> if <see cref="IsRoot"/> is true.</remarks>
        public Vector3 Position
        {
            get => _trs.Position;
            set
            {
                if(_trs.SetPosition(value, out var changed)) {
                    changed.InvokeIgnoreException(this);
                }
            }
        }

        /// <summary>Get or set world position.</summary>
        /// <remarks>Both getter and setter are O(N); N is the number of all parents from self to the root object.</remarks>
        public Vector3 WorldPosition
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return IsRoot ? Position : Calc(this);

                static Vector3 Calc(Positionable source)
                {
                    var wPos = source.Position;
                    while(!source.IsRoot) {
                        source = source._parent!;
                        wPos += source.Position;
                    }
                    return wPos;
                }
            }
            set => Position += value - WorldPosition;
        }

        /// <summary>Get or set scale of <see cref="Positionable"/>.</summary>
        public Vector3 Scale
        {
            get => _trs.Scale;
            set
            {
                if(_trs.SetScale(value, out var changed)) {
                    changed.InvokeIgnoreException(this);
                }
            }
        }

        public Event<Positionable> PositionChanged => _trs.PositionChanged;
        public Event<Positionable> RotationChanged => _trs.RotationChanged;
        public Event<Positionable> ScaleChanged => _trs.ScaleChanged;
        public Event<Positionable> ParentChanged => _parentChanged.Event;

        public Positionable() : this(FrameObjectInstanceType.Positionable)
        {
        }

        private protected Positionable(FrameObjectInstanceType instanceType) : base(instanceType)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Matrix4 GetSelfModelMatrix() => _trs.GetTransform();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Matrix4 GetModelMatrix()
        {
            if(_modelCache.HasValue) {
                return _modelCache.Value;
            }
            return CalcAndCache(this);

            [MethodImpl(MethodImplOptions.NoInlining)]
            static Matrix4 CalcAndCache(Positionable self)
            {
                var parent = self.Parent;
                var mat = (parent == null) ? self.GetSelfModelMatrix() : parent.GetModelMatrix() * self.GetSelfModelMatrix();
                self._modelCache = mat;
                return mat;
            }
        }

        /// <summary>Translate the <see cref="Positionable"/>.</summary>
        /// <param name="x">translation of x</param>
        /// <param name="y">translation of y</param>
        /// <param name="z">translation of z</param>
        public void Translate(float x, float y, float z)
        {
            Position += new Vector3(x, y, z);
        }

        /// <summary>Translate the <see cref="Positionable"/>.</summary>
        /// <param name="vector">translation vector</param>
        public void Translate(in Vector3 vector)
        {
            Position += vector;
        }

        /// <summary>Multiply scale.</summary>
        /// <param name="ratio">ratio</param>
        public void MultiplyScale(float ratio)
        {
            Scale *= ratio;
        }

        /// <summary>Multiply scale.</summary>
        /// <param name="x">ratio of x</param>
        /// <param name="y">ratio of y</param>
        /// <param name="z">ratio of z</param>
        public void MultiplyScale(float x, float y, float z)
        {
            Scale *= new Vector3(x, y, z);
        }

        /// <summary>Multiply scale.</summary>
        /// <param name="ratio">ratio</param>
        public void MultiplyScale(in Vector3 ratio)
        {
            Scale *= ratio;
        }

        /// <summary>Rotate the <see cref="Positionable"/> by axis and angle.</summary>
        /// <param name="axis">Axis of rotation</param>
        /// <param name="angle">Angle of rotation [radian]</param>
        public void Rotate(in Vector3 axis, float angle)
        {
            Rotation = Quaternion.FromAxisAngle(axis, angle) * Rotation;
        }

        /// <summary>Rotate the <see cref="Positionable"/> by <see cref="Quaternion"/>.</summary>
        /// <param name="quaternion"><see cref="Quaternion"/></param>
        public void Rotate(in Quaternion quaternion)
        {
            Rotation = quaternion * Rotation;
        }

        /// <summary>Get all children recursively by DFS (depth-first search).</summary>
        /// <returns>All offspring</returns>
        public IEnumerable<Positionable> GetOffspring()
        {
            foreach(var child in _childrenCore) {
                yield return child;
                foreach(var offspring in child.GetOffspring()) {
                    yield return offspring;
                }
            }
        }

        /// <summary>Get all parents recursively. Iterate objects from the parent of self to root.</summary>
        /// <returns>All parents</returns>
        public PositionableAncestors GetAncestors() => new PositionableAncestors(this);

        /// <summary>Get the root <see cref="Positionable"/>.</summary>
        /// <returns>The root object</returns>
        public Positionable GetRoot()
        {
            return IsRoot ? this : SerachRoot(this);

            static Positionable SerachRoot(Positionable source)
            {
                var obj = source;
                while(!obj.IsRoot) {
                    obj = obj._parent!;
                }
                return obj;
            }
        }

        internal virtual void RenderRecursively(in Matrix4 modelParent, in Matrix4 view, in Matrix4 projection)
        {
            var children = Children.AsSpan();
            if(children.IsEmpty) {
                return;
            }
            var model = GetModelMatrix();
            foreach(var child in children) {
                child.RenderRecursively(model, view, projection);
            }
        }

        internal virtual void RenderShadowMapRecursively(in Matrix4 modelParent, CascadedShadowMap shadowMap)
        {
            var children = Children.AsSpan();
            if(children.IsEmpty) {
                return;
            }
            var model = GetModelMatrix();
            foreach(var child in children) {
                child.RenderShadowMapRecursively(model, shadowMap);
            }
        }
    }

    public readonly struct PositionableAncestors : IEnumerable<Positionable>
    {
        private readonly Positionable _target;

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Don't use default constructor.", true)]
        public PositionableAncestors() => throw new NotSupportedException("Don't use default constructor.");

        public PositionableAncestors(Positionable target) => _target = target;

        public Enumerator GetEnumerator() => new Enumerator(_target);

        IEnumerator<Positionable> IEnumerable<Positionable>.GetEnumerator() => new EnumeratorClass(_target);
        IEnumerator IEnumerable.GetEnumerator() => new EnumeratorClass(_target);

        public struct Enumerator : IEnumerator<Positionable>
        {
            private Positionable? _current;

            [EditorBrowsable(EditorBrowsableState.Never)]
            [Obsolete("Don't use default constructor.", true)]
            public Enumerator() => throw new NotSupportedException("Don't use default constructor.");

            internal Enumerator(Positionable target)
            {
                _current = target;
            }

            public Positionable Current => _current!;

            object IEnumerator.Current => Current;

            public void Dispose() { }

            public bool MoveNext()
            {
                _current = _current!.Parent;
                return _current != null;
            }

            public void Reset() => throw new NotSupportedException();
        }

        private sealed class EnumeratorClass : IEnumerator<Positionable>
        {
            private Enumerator _e;  // Mutable object. Don't make it readonly.

            public EnumeratorClass(Positionable target) => _e = new Enumerator(target);

            public Positionable Current => _e.Current;

            object IEnumerator.Current => Current;

            public void Dispose() => _e.Dispose();

            public bool MoveNext() => _e.MoveNext();

            public void Reset() => _e.Reset();
        }
    }
}
