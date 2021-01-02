#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Elffy.Core
{
    /// <summary>Base class which exists in space. That provides position, size and rotation.</summary>
    public abstract class Positionable : ComponentOwner
    {
        private Quaternion _ratation = Quaternion.Identity;
        private Vector3 _scale = Vector3.One;
        private Vector3 _position;
        private readonly PositionableCollection _children;
        private Positionable? _parent;

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
        public PositionableCollection Children => _children;

        /// <summary>Get whether the <see cref="Positionable"/> is a root object in the tree.</summary>
        public bool IsRoot => _parent is null;

        /// <summary>Get whether the <see cref="Positionable"/> has any children.</summary>
        public bool HasChild => _children.Count > 0;

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

        public Positionable()
        {
            _children = new PositionableCollection(this);
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
            foreach(var child in _children) {
                yield return child;
                foreach(var offspring in child.GetOffspring()) {
                    yield return offspring;
                }
            }
        }

        /// <summary>Get all parents recursively. Iterate objects from the parent of self to root.</summary>
        /// <returns>All parents</returns>
        public IEnumerable<Positionable> GetAncestor()
        {
            Positionable? target = this;
            while(target!.IsRoot == false) {
                yield return target.Parent!;
                target = target.Parent;
            }
        }

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
    }
}
