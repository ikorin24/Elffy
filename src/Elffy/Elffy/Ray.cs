using Elffy.Exceptions;
using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elffy.Elffy
{
    /// <summary>3次元空間上の直線を表すクラス</summary>
    public class Ray
    {
        /// <summary>この直線が通る点</summary>
        public Vector3 Intercept
        {
            get => _intercept;
            set
            {
                ThrowIfContainsInifinityOrNaN(value);
                _intercept = value;
            }
        }
        private Vector3 _intercept;

        /// <summary>この直線の方向</summary>
        public Vector3 Direction
        {
            get => _direction;
            set
            {
                ArgumentChecker.ThrowIf(value.Length == 0, new ArgumentException($"A vector which length is 0 can not be '{Direction}'"));
                ThrowIfContainsInifinityOrNaN(value);
                _direction = value;
            }
        }
        private Vector3 _direction = Vector3.UnitZ;

        /// <summary>通過点と方向を指定して3次元空間上の直線を生成します</summary>
        /// <param name="intercept">この直線が通る点</param>
        /// <param name="direction">この直線の方向</param>
        public Ray(Vector3 intercept, Vector3 direction)
        {
            Intercept = intercept;
            Direction = direction;
        }

        /// <summary>通過点と方向を指定して3次元空間上の直線を生成します</summary>
        /// <param name="intercept">直線の通過点</param>
        /// <param name="direction">直線の方向</param>
        /// <returns>直線</returns>
        public static Ray FromPointAndDirection(Vector3 intercept, Vector3 direction) => new Ray(intercept, direction);

        /// <summary>2点を指定して、3次元空間上の直線を生成します</summary>
        /// <param name="point1">直線の通過点1</param>
        /// <param name="point2">直線の通過点2</param>
        /// <returns>直線</returns>
        public static Ray FromTwoPoints(Vector3 point1, Vector3 point2)
        {
            var direction = point2 - point1;
            return new Ray(point1, direction);
        }

        /// <summary>この直線と特定のX座標のYZ平面との交点座標を取得します</summary>
        /// <param name="x">X座標</param>
        /// <returns>交点座標</returns>
        public Vector3 SpecifiedX(float x)
        {
            ThrowIfInifinityOrNaN(x);
            var tmp = (x - Intercept.X) / Direction.X;
            var y = tmp * Direction.Y - Intercept.Y;
            var z = tmp * Direction.Z - Intercept.Z;
            return new Vector3(x, y, z);
        }

        /// <summary>この直線と特定のY座標のXZ平面の交点座標を取得します</summary>
        /// <param name="y">Y座標</param>
        /// <returns>交点座標</returns>
        public Vector3 SpecifiedY(float y)
        {
            ThrowIfInifinityOrNaN(y);
            var tmp = (y - Intercept.Y) / Direction.Y;
            var x = tmp * Direction.X - Intercept.X;
            var z = tmp * Direction.Z - Intercept.Z;
            return new Vector3(x, y, z);
        }

        /// <summary>この直線と特定のZ座標のXY平面との交点座標を取得します</summary>
        /// <param name="z">Z座標</param>
        /// <returns>交点直線</returns>
        public Vector3 SpecifiedZ(float z)
        {
            ThrowIfInifinityOrNaN(z);
            var tmp = (z - Intercept.Z) / Direction.Z;
            var x = tmp * Direction.X - Intercept.X;
            var y = tmp * Direction.Y - Intercept.Y;
            return new Vector3(x, y, z);
        }

        /// <summary>各軸の値が +∞ または -∞ または NaN なら例外を投げます</summary>
        /// <param name="value">値</param>
        private void ThrowIfContainsInifinityOrNaN(Vector3 value)
        {
            ArgumentChecker.ThrowIf(float.IsNaN(value.X) || float.IsNaN(value.Y) || float.IsNaN(value.Z) ||
                                     float.IsInfinity(value.X) || float.IsInfinity(value.Y) || float.IsInfinity(value.Z) ||
                                     float.IsNegativeInfinity(value.X) || float.IsNegativeInfinity(value.Y) || float.IsNegativeInfinity(value.Z),
                new ArgumentException($"value of X, Y, or Z is {float.NaN}, {float.PositiveInfinity}, or {float.NegativeInfinity}"));
        }

        /// <summary>+∞ または -∞ または NaN なら例外を投げます</summary>
        /// <param name="value">値</param>
        private void ThrowIfInifinityOrNaN(float value)
        {
            ArgumentChecker.ThrowIf(float.IsNaN(value) || float.IsInfinity(value) || float.IsNegativeInfinity(value),
                new ArgumentException($"value is {float.NaN}, {float.PositiveInfinity}, or {float.NegativeInfinity}"));
        }
    }
}
