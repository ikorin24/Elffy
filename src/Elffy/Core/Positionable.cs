using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elffy.Core
{
    /// <summary>
    /// 空間に置くことができるオブジェクトの基底クラス<para/>
    /// 座標・サイズ・回転等に関する操作を提供します。<para/>
    /// </summary>
    public abstract class Positionable : FrameObject
    {
        #region Proeprty
        /// <summary>オブジェクトの回転を表すクオータニオン</summary>
        public Quaternion Rotation { get; set; } = Quaternion.Identity;

        public ObjectLayer Layer { get; set; }

        #region Position
        /// <summary>オブジェクトの座標</summary>
        public Vector3 Position
        {
            get => _position;
            set { _position = value; }
        }
        internal Vector3 _position;
        #endregion

        #region PositionX
        /// <summary>オブジェクトのX座標</summary>
        public float PositionX
        {
            get => _position.X;
            set { _position.X = value; }
        }
        #endregion

        #region PositionY
        /// <summary>オブジェクトのY座標</summary>
        public float PositionY
        {
            get => _position.Y;
            set { _position.Y = value; }
        }
        #endregion

        #region PositionZ
        /// <summary>オブジェクトのZ座標</summary>
        public float PositionZ
        {
            get => _position.Z;
            set { _position.Z = value; }
        }
        #endregion

        #region Scale
        /// <summary>オブジェクトの拡大率</summary>
        public Vector3 Scale
        {
            get => _scale;
            set { _scale = value; }
        }
        internal Vector3 _scale = Vector3.One;
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
    }
}
