#nullable enable
using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Elffy.Core
{
    /// <summary>
    /// 空間に置くことができるオブジェクトの基底クラス<para/>
    /// 座標・サイズ・回転等に関する操作を提供します。<para/>
    /// </summary>
    public abstract class Positionable : ComponentOwner
    {
        #region Proeprty
        /// <summary>オブジェクトの回転を表すクオータニオン</summary>
        public Quaternion Rotation { get; set; } = Quaternion.Identity;

        /// <summary>この <see cref="Positionable"/> のツリー構造の親を取得します</summary>
        public Positionable? Parent
        {
            get => _parent;
            internal set
            {
                if(_parent == null) {
                    _parent = value;
                }
                else if(_parent != null && value == null) {
                    _parent = value;
                }
                else { throw new InvalidOperationException($"The instance is already a child of another object. Can not has multi parents."); }
            }
        }
        private Positionable? _parent;

        /// <summary>この <see cref="Positionable"/> のツリー構造の子要素を取得します</summary>
        public PositionableCollection Children { get; }

        /// <summary>この <see cref="Positionable"/> がツリー構造の Root かどうかを取得します</summary>
        public bool IsRoot => Parent == null;

        /// <summary>この <see cref="Positionable"/> が子要素の <see cref="Positionable"/> を持っているかどうかを取得します</summary>
        public bool HasChild => Children.Count > 0;

        #region Position
        /// <summary>
        /// オブジェクトのローカル座標<para/>
        /// <see cref="IsRoot"/> が true の場合は <see cref="WorldPosition"/> と同じ値。false の場合は親の <see cref="Position"/> を基準とした相対座標。
        /// </summary>
        public Vector3 Position
        {
            get => _position;
            set
            {
                var vec = value - _position;
                _position += vec;
                _worldPosition += vec;
                foreach(var child in GetOffspring()) {
                    child._worldPosition += vec;
                }
            }
        }
        private Vector3 _position;
        #endregion

        #region PositionX
        /// <summary>オブジェクトのX座標</summary>
        public float PositionX
        {
            get => _position.X;
            set
            {
                var diff = value - _position.X;
                _position.X += diff;
                _worldPosition.X += diff;
                foreach(var child in GetOffspring()) {
                    child._worldPosition.X += diff;
                }
            }
        }
        #endregion

        #region PositionY
        /// <summary>オブジェクトのY座標</summary>
        public float PositionY
        {
            get => _position.Y;
            set
            {
                var diff = value - _position.Y;
                _position.Y += diff;
                _worldPosition.Y += diff;
                foreach(var child in GetOffspring()) {
                    child._worldPosition.Y += diff;
                }
            }
        }
        #endregion

        #region PositionZ
        /// <summary>オブジェクトのZ座標</summary>
        public float PositionZ
        {
            get => _position.Z;
            set
            {
                var diff = value - _position.Z;
                _position.Z += diff;
                _worldPosition.Z += diff;
                foreach(var child in GetOffspring()) {
                    child._worldPosition.Z += diff;
                }
            }
        }
        #endregion

        #region WorldPosition
        /// <summary>オブジェクトのワールド座標</summary>
        public Vector3 WorldPosition
        {
            get => _worldPosition;
            set
            {
                var vec = value - _worldPosition;
                _worldPosition += vec;
                _position += vec;
                foreach(var child in GetOffspring()) {
                    child._worldPosition += vec;
                }
            }
        }
        private Vector3 _worldPosition;
        #endregion

        #region WorldPositionX
        /// <summary>ワールド座標のX座標</summary>
        public float WorldPositionX
        {
            get => _worldPosition.X;
            set
            {
                var diff = value - _worldPosition.X;
                _worldPosition.X += diff;
                _position.X += diff;
                foreach(var child in GetOffspring()) {
                    child._worldPosition.X += diff;
                }
            }
        }
        #endregion

        #region WorldPositionY
        /// <summary>ワールド座標のY座標</summary>
        public float WorldPositionY
        {
            get => _worldPosition.Y;
            set
            {
                var diff = value - _worldPosition.Y;
                _worldPosition.Y += diff;
                _position.Y += diff;
                foreach(var child in GetOffspring()) {
                    child._worldPosition.Y += diff;
                }
            }
        }
        #endregion

        #region WorldPositionZ
        /// <summary>ワールド座標のZ座標</summary>
        public float WorldPositionZ
        {
            get => _worldPosition.Z;
            set
            {
                var diff = value - _worldPosition.Z;
                _worldPosition.Z += diff;
                _position.Z += diff;
                foreach(var child in GetOffspring()) {
                    child._worldPosition.Z += diff;
                }
            }
        }
        #endregion

        #region Scale
        /// <summary>オブジェクトの拡大率</summary>
        public Vector3 Scale
        {
            get => _scale;
            set { _scale = value; }
        }
        private Vector3 _scale = Vector3.One;
        #endregion

        #region ScaleX
        /// <summary>x軸方向の拡大率</summary>
        public float ScaleX
        {
            get => _scale.X;
            set { _scale.X = value; }
        }
        #endregion

        #region ScaleY
        /// <summary>y軸方向の拡大率</summary>
        public float ScaleY
        {
            get => _scale.Y;
            set { _scale.Y = value; }
        }
        #endregion

        #region ScaleZ
        /// <summary>z軸方向の拡大率</summary>
        public float ScaleZ
        {
            get => _scale.Z;
            set { _scale.Z = value; }
        }
        #endregion
        #endregion

        public Positionable()
        {
            Children = new PositionableCollection(this);
            Activated += OnActivated;
        }

        #region Translate
        /// <summary>オブジェクトを移動させます</summary>
        /// <param name="x">x軸方向移動量</param>
        /// <param name="y">y軸方向移動量</param>
        /// <param name="z">z軸方向移動量</param>
        public void Translate(float x, float y, float z)
        {
            Position += new Vector3(x, y, z);
        }

        /// <summary>オブジェクトを移動させます</summary>
        /// <param name="vector">移動ベクトル</param>
        public void Translate(Vector3 vector)
        {
            Position += vector;
        }
        #endregion

        #region MultiplyScale
        /// <summary>オブジェクトのサイズを変更します</summary>
        /// <param name="scale">倍率</param>
        public void MultiplyScale(float scale)
        {
            _scale *= scale;
        }

        /// <summary>オブジェクトのサイズを変更します</summary>
        /// <param name="x">x軸方向の倍率</param>
        /// <param name="y">y軸方向の倍率</param>
        /// <param name="z">z軸方向の倍率</param>
        public void MultiplyScale(float x, float y, float z)
        {
            _scale *= new Vector3(x, y, z);
        }
        #endregion

        #region Rotate
        /// <summary>オブジェクトを回転させます</summary>
        /// <param name="axis">回転軸</param>
        /// <param name="angle">回転角(ラジアン)</param>
        public void Rotate(Vector3 axis, float angle) => Rotate(Quaternion.FromAxisAngle(axis, angle));

        /// <summary>オブジェクトを回転させます</summary>
        /// <param name="quaternion">回転させるクオータニオン</param>
        public void Rotate(Quaternion quaternion)
        {
            Rotation *= quaternion;
        }
        #endregion

        /// <summary>このオブジェクトの <see cref="Children"/> 以下に存在する全ての子孫を取得します。列挙順は深さ優先探索 (DFS; depth-first search) です。</summary>
        /// <returns>全ての子孫オブジェクト</returns>
        public IEnumerable<Positionable> GetOffspring()
        {
            foreach(var child in Children) {
                yield return child;
                foreach(var offspring in child.GetOffspring()) {
                    yield return offspring;
                }
            }
        }

        /// <summary>このオブジェクトの <see cref="Parent"/> 以上に存在する全ての先祖を取得します。列挙順は自身の親からRoot方向への順です。</summary>
        /// <returns>全ての先祖オブジェクト</returns>
        public IEnumerable<Positionable> GetAncestor()
        {
            Positionable? target = this;
            while(target!.IsRoot == false) {
                yield return target.Parent!;
                target = target.Parent;
            }
        }

        /// <summary>このオブジェクトの Root オブジェクトを取得します。</summary>
        /// <returns>Root オブジェクト</returns>
        public Positionable GetRoot()
        {
            if(IsRoot) { return this; }
            return GetAncestor().Last();
        }

        private void OnActivated(FrameObject frameObject)
        {
            var layer = Layer!;
            foreach(var offspring in GetOffspring()) {
                offspring.Activate(layer);
            }
        }
    }
}
