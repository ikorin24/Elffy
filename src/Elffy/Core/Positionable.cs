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
        /// <summary>
        /// オブジェクトの移動・拡大を表すマトリクス<para/>
        /// ※回転は <see cref="Rotation"/> によって表されます
        /// </summary>
        internal Matrix4 ModelMatrix => _modelMatrix;
        private Matrix4 _modelMatrix = Matrix4.Identity;

        /// <summary>オブジェクトの回転を表すクオータニオン</summary>
        public Quaternion Rotation { get; set; } = Quaternion.Identity;

        public ObjectLayer Layer { get; set; }

        /// <summary>オブジェクトの座標</summary>
        public Vector3 Position
        {
            get { return _modelMatrix.Row3.Xyz; }
            set { _modelMatrix.Row3.Xyz = value; }
        }

        /// <summary>x軸方向の拡大率</summary>
        public float ScaleX
        {
            get { return _modelMatrix.Row0.X; }
            set { _modelMatrix.Row0.X = value; }
        }

        /// <summary>y軸方向の拡大率</summary>
        public float ScaleY
        {
            get { return _modelMatrix.Row1.Y; }
            set { _modelMatrix.Row1.Y = value; }
        }

        /// <summary>z軸方向の拡大率</summary>
        public float ScaleZ
        {
            get { return _modelMatrix.Row2.Z; }
            set { _modelMatrix.Row2.Z = value; }
        }
        #endregion

        #region Translate
        /// <summary>オブジェクトを移動させます</summary>
        /// <param name="x">x軸方向移動量</param>
        /// <param name="y">y軸方向移動量</param>
        /// <param name="z">z軸方向移動量</param>
        public void Translate(float x, float y, float z)
        {
            if(x == 0 && y == 0 && z == 0) { return; }
            _modelMatrix *= new Matrix4(1, 0, 0, 0,
                                      0, 1, 0, 0,
                                      0, 0, 1, 0,
                                      x, y, z, 1);
        }

        /// <summary>オブジェクトを移動させます</summary>
        /// <param name="vector">移動ベクトル</param>
        public void Translate(Vector3 vector)
        {
            _modelMatrix *= new Matrix4(1, 0, 0, 0,
                                        0, 1, 0, 0,
                                        0, 0, 1, 0,
                                        vector.X, vector.Y, vector.Z, 1);
        }
        #endregion

        #region MultiplyScale
        /// <summary>オブジェクトのサイズを変更します</summary>
        /// <param name="scale">倍率</param>
        public void MultiplyScale(float scale)
        {
            _modelMatrix *= new Matrix4(scale, 0, 0, 0,
                                      0, scale, 0, 0,
                                      0, 0, scale, 0,
                                      0, 0, 0, 1);
        }

        /// <summary>オブジェクトのサイズを変更します</summary>
        /// <param name="x">x軸方向の倍率</param>
        /// <param name="y">y軸方向の倍率</param>
        /// <param name="z">z軸方向の倍率</param>
        public void MultiplyScale(float x, float y, float z)
        {
            _modelMatrix *= new Matrix4(x, 0, 0, 0,
                                      0, y, 0, 0,
                                      0, 0, z, 0,
                                      0, 0, 0, 1);
        }

        /// <summary>オブジェクトのサイズを変更します</summary>
        /// <param name="x">x軸方向の倍率</param>
        /// <param name="y">y軸方向の倍率</param>
        /// <param name="z">z軸方向の倍率</param>
        /// <param name="scaleOrigin">拡大の中心座標</param>
        public void MultiplyScale(float x, float y , float z, Vector3 scaleOrigin)
        {
            Translate(-scaleOrigin.X, -scaleOrigin.Y, -scaleOrigin.Z);
            MultiplyScale(x, y , z);
            Translate(scaleOrigin.X, scaleOrigin.Y, scaleOrigin.Z);
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
