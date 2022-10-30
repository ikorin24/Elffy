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
        private Quaternion _ratation = Quaternion.Identity;
        private Vector3 _scale = Vector3.One;
        private Vector3 _position;
        private ArrayPooledListCore<Positionable> _childrenCore = new();    // mutable object, don't make it readonly
        private Positionable? _parent;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        internal ref ArrayPooledListCore<Positionable> ChildrenCore => ref _childrenCore;   // don't make it ref readonly

        /// <summary>Get or set <see cref="Quaternion"/> of rotation.</summary>
        public ref Quaternion Rotation => ref _ratation;

        /// <summary>Get parent of the <see cref="Positionable"/>.</summary>
        public Positionable? Parent
        {
            get => _parent;
            internal set
            {
                if(_parent is null || value is null) {
                    _parent = value;
                }
                else { ThrowAlreadyHasParent(); }

                static void ThrowAlreadyHasParent() => throw new InvalidOperationException($"The instance is already a child of another object. Can not has multi parents.");
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
        public ref Vector3 Position => ref _position;

        /// <summary>Get or set world position.</summary>
        /// <remarks>Both getter and setter are O(N); N is the number of all parents from self to the root object.</remarks>
        public Vector3 WorldPosition
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return IsRoot ? _position : Calc(this);

                static Vector3 Calc(Positionable source)
                {
                    var wPos = source._position;
                    while(!source.IsRoot) {
                        source = source._parent!;
                        wPos += source._position;
                    }
                    return wPos;
                }
            }
            set => _position += value - WorldPosition;
        }

        /// <summary>Get or set scale of <see cref="Positionable"/>.</summary>
        public ref Vector3 Scale => ref _scale;

        public Positionable() : base(FrameObjectInstanceType.Positionable)
        {
        }

        private protected Positionable(FrameObjectInstanceType instanceType) : base(instanceType)
        {
        }

        internal static Matrix4 CalcModelMatrix(in Vector3 position, in Quaternion rotation, in Vector3 scale)
        {
            // TODO: optimize
            return position.ToTranslationMatrix4() * rotation.ToMatrix4() * scale.ToScaleMatrix4();
        }

        public Matrix4 GetSelfModelMatrix() => CalcModelMatrix(in _position, in _ratation, in _scale);

        public Matrix4 GetModelMatrix()
        {
            var model = GetSelfModelMatrix();
            var parent = _parent;
            if(parent == null) {
                return model;
            }
            while(true) {
                model = parent.GetSelfModelMatrix() * model;
                parent = parent.Parent;
                if(parent == null) {
                    return model;
                }
            }
        }

        /// <summary>Translate the <see cref="Positionable"/>.</summary>
        /// <param name="x">translation of x</param>
        /// <param name="y">translation of y</param>
        /// <param name="z">translation of z</param>
        public void Translate(float x, float y, float z)
        {
            _position.X += x;
            _position.Y += y;
            _position.Z += z;
        }

        /// <summary>Translate the <see cref="Positionable"/>.</summary>
        /// <param name="vector">translation vector</param>
        public void Translate(in Vector3 vector)
        {
            _position += vector;
        }

        /// <summary>Multiply scale.</summary>
        /// <param name="ratio">ratio</param>
        public void MultiplyScale(float ratio)
        {
            _scale *= ratio;
        }

        /// <summary>Multiply scale.</summary>
        /// <param name="x">ratio of x</param>
        /// <param name="y">ratio of y</param>
        /// <param name="z">ratio of z</param>
        public void MultiplyScale(float x, float y, float z)
        {
            _scale.X *= x;
            _scale.Y *= y;
            _scale.Z *= z;
        }

        /// <summary>Rotate the <see cref="Positionable"/> by axis and angle.</summary>
        /// <param name="axis">Axis of rotation</param>
        /// <param name="angle">Angle of rotation [radian]</param>
        public void Rotate(in Vector3 axis, float angle) => Rotate(Quaternion.FromAxisAngle(axis, angle));

        /// <summary>Rotate the <see cref="Positionable"/> by <see cref="Quaternion"/>.</summary>
        /// <param name="quaternion"><see cref="Quaternion"/></param>
        public void Rotate(in Quaternion quaternion)
        {
            _ratation = quaternion * _ratation;
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

            var withoutScale = modelParent * Position.ToTranslationMatrix4() * Rotation.ToMatrix4();
            foreach(var child in children) {
                child.RenderRecursively(withoutScale, view, projection);
            }
        }

        internal virtual void RenderShadowMapRecursively(in Matrix4 modelParent, in Matrix4 lightViewProjection)
        {
            var children = Children.AsSpan();
            if(children.IsEmpty) {
                return;
            }
            var model = modelParent * GetSelfModelMatrix();
            foreach(var child in children) {
                child.RenderShadowMapRecursively(model, lightViewProjection);
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
