#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

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
        public Quaternion Rotation { get; private set; } = Quaternion.Identity;

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

        /// <summary>
        /// オブジェクトのローカル座標<para/>
        /// <see cref="IsRoot"/> が true の場合は <see cref="WorldPosition"/> と同じ値。false の場合は親の <see cref="Position"/> を基準とした相対座標。
        /// </summary>
        public Vector3 Position
        {
            get => _position;
            set => _position = value;
        }
        private Vector3 _position;

        /// <summary>オブジェクトのX座標</summary>
        public float PositionX
        {
            get => _position.X;
            set => _position.X = value;
        }

        /// <summary>オブジェクトのY座標</summary>
        public float PositionY
        {
            get => _position.Y;
            set => _position.Y = value;
        }

        /// <summary>オブジェクトのZ座標</summary>
        public float PositionZ
        {
            get => _position.Z;
            set => _position.Z = value;
        }

        /// <summary>オブジェクトのワールド座標。get/set ともに Root までの親の数 N に対し O(N)</summary>
        public Vector3 WorldPosition
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var worldPos = _position;
                var current = this;
                while(!current.IsRoot) {
                    current = current.Parent!;
                    worldPos += current._position;
                }
                return worldPos;
            }
            set => _position += value - WorldPosition;
        }

        /// <summary>ワールド座標のX座標。get/set ともに Root までの親の数 N に対し O(N)</summary>
        public float WorldPositionX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var worldPosX = _position.X;
                var current = this;
                while(!current.IsRoot) {
                    current = current.Parent!;
                    worldPosX += current._position.X;
                }
                return worldPosX;
            }
            set => _position.X += value - WorldPositionX;
        }

        /// <summary>ワールド座標のY座標。get/set ともに Root までの親の数 N に対し O(N)</summary>
        public float WorldPositionY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var worldPosY = _position.Y;
                var current = this;
                while(!current.IsRoot) {
                    current = current.Parent!;
                    worldPosY += current._position.Y;
                }
                return worldPosY;
            }
            set => _position.Y += value - WorldPositionY;
        }

        /// <summary>ワールド座標のZ座標。get/set ともに Root までの親の数 N に対し O(N)</summary>
        public float WorldPositionZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var worldPosZ = _position.Z;
                var current = this;
                while(!current.IsRoot) {
                    current = current.Parent!;
                    worldPosZ += current._position.Z;
                }
                return worldPosZ;
            }
            set => _position.Z += value - WorldPositionZ;
        }

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
        }

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
            _scale = _scale.Mult(new Vector3(x, y, z));
        }

        /// <summary>オブジェクトを回転させます</summary>
        /// <param name="axis">回転軸</param>
        /// <param name="angle">回転角(ラジアン)</param>
        public void Rotate(Vector3 axis, float angle) => Rotate(new Quaternion(axis, angle));

        /// <summary>オブジェクトを回転させます</summary>
        /// <param name="quaternion">回転させるクオータニオン</param>
        public void Rotate(Quaternion quaternion)
        {
            Rotation = quaternion * Rotation;
        }

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
    }
}
