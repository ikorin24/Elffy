#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Elffy.Core
{
    /// <summary>
    /// 空間に置くことができるオブジェクトの基底クラス<para/>
    /// 座標・サイズ・回転等に関する操作を提供します。<para/>
    /// </summary>
    public abstract class Positionable : ComponentOwner
    {
        private Quaternion _ratation = Quaternion.Identity;
        private Vector3 _scale = Vector3.One;
        private Vector3 _position;
        private readonly PositionableCollection _children;
        private Positionable? _parent;

        #region Proeprty
        /// <summary>オブジェクトの回転を表すクオータニオン</summary>
        public ref Quaternion Rotation => ref _ratation;

        /// <summary>この <see cref="Positionable"/> のツリー構造の親を取得します</summary>
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

        /// <summary>この <see cref="Positionable"/> のツリー構造の子要素を取得します</summary>
        public PositionableCollection Children => _children;

        /// <summary>この <see cref="Positionable"/> がツリー構造の Root かどうかを取得します</summary>
        public bool IsRoot => _parent is null;

        /// <summary>この <see cref="Positionable"/> が子要素の <see cref="Positionable"/> を持っているかどうかを取得します</summary>
        public bool HasChild => Children.Count > 0;

        /// <summary>
        /// オブジェクトのローカル座標<para/>
        /// <see cref="IsRoot"/> が true の場合は <see cref="WorldPosition"/> と同じ値。false の場合は親の <see cref="Position"/> を基準とした相対座標。
        /// </summary>
        public ref Vector3 Position => ref _position;

        /// <summary>オブジェクトのワールド座標。get/set ともに Root までの親の数 N に対し O(N)</summary>
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

        /// <summary>ワールド座標のX座標。get/set ともに Root までの親の数 N に対し O(N)</summary>
        public float WorldPositionX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return IsRoot ? _position.X : Calc(this);

                static float Calc(Positionable source)
                {
                    var wPosX = source._position.X;
                    while(!source.IsRoot) {
                        source = source._parent!;
                        wPosX += source._position.X;
                    }
                    return wPosX;
                }
            }
            set => _position.X += value - WorldPositionX;
        }

        /// <summary>ワールド座標のY座標。get/set ともに Root までの親の数 N に対し O(N)</summary>
        public float WorldPositionY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return IsRoot ? _position.Y : Calc(this);

                static float Calc(Positionable source)
                {
                    var wPosY = source._position.Y;
                    while(!source.IsRoot) {
                        source = source._parent!;
                        wPosY += source._position.Y;
                    }
                    return wPosY;
                }
            }
            set => _position.Y += value - WorldPositionY;
        }

        /// <summary>ワールド座標のZ座標。get/set ともに Root までの親の数 N に対し O(N)</summary>
        public float WorldPositionZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return IsRoot ? _position.Z : Calc(this);

                static float Calc(Positionable source)
                {
                    var wPosZ = source._position.Z;
                    while(!source.IsRoot) {
                        source = source._parent!;
                        wPosZ += source._position.Z;
                    }
                    return wPosZ;
                }
            }
            set => _position.Z += value - WorldPositionZ;
        }

        /// <summary>オブジェクトの拡大率</summary>
        public ref Vector3 Scale => ref _scale;
        #endregion

        public Positionable()
        {
            _children = new PositionableCollection(this);
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
        public void Translate(in Vector3 vector)
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
        public void Rotate(in Vector3 axis, float angle) => Rotate(Quaternion.FromAxisAngle(axis, angle));

        /// <summary>オブジェクトを回転させます</summary>
        /// <param name="quaternion">回転させるクオータニオン</param>
        public void Rotate(in Quaternion quaternion)
        {
            _ratation = quaternion * _ratation;
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
