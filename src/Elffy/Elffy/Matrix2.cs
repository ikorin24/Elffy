#nullable enable
using System;
using System.Runtime.InteropServices;
using Elffy.Mathmatics;

namespace Elffy
{
    /// <summary>Matrix of 2x2</summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct Matrix2 : IEquatable<Matrix2>
    {
        // =================================================
        // [Field Order]
        // Field order is row-major order.
        // | M00 M01 |
        // | M10 M11 |
        //
        // [Mathmatical Order]
        // Mathmatical order is column-major order.
        // This is popular mathmatical way.
        // 
        // (ex) vector transformation
        // Multiply matrix from forward
        // vec1 = matrix * vec0
        //
        //  | x1 | = | M00 M01 | * | x0 |
        //  | y1 |   | M10 M11 |   | y0 |
        // 
        //         = | M00 * x0 + M01 * y0 |
        //           | M10 * x0 + M11 * y0 |
        // =================================================

        [FieldOffset(0)]
        public float M00;
        [FieldOffset(4)]
        public float M01;
        [FieldOffset(8)]
        public float M10;
        [FieldOffset(12)]
        public float M11;

        public Matrix2(float m00, float m01, float m10, float m11)
        {
            M00 = m00;
            M01 = m01;
            M10 = m10;
            M11 = m11;
        }

        public void Transpose()
        {
            var tmp = M01;
            M01 = M10;
            M10 = tmp;
        }

        public readonly Matrix2 Transposed()
        {
            return new Matrix2(M00, M10, M01, M11);
        }

        public static Matrix2 GetRotateMatrix(float theta)
        {
            var cos = MathTool.Cos(theta);
            var sin = MathTool.Sin(theta);
            return new Matrix2(cos, -sin,
                               sin, cos);
        }

        public override bool Equals(object? obj)
        {
            return obj is Matrix2 matrix && Equals(matrix);
        }

        public bool Equals(Matrix2 other)
        {
            return M00 == other.M00 &&
                   M01 == other.M01 &&
                   M10 == other.M10 &&
                   M11 == other.M11;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(M00, M01, M10, M11);
        }

        public static Matrix2 operator *(in Matrix2 m1, in Matrix2 m2)
        {
            return new Matrix2(m1.M00 * m2.M00 + m1.M01 * m2.M10,    m1.M00 * m2.M01 + m1.M01 * m2.M11,
                               m1.M10 * m2.M00 + m1.M11 * m2.M10,    m1.M10 * m2.M01 + m1.M11 * m2.M11);
        }

        public static Vector2 operator *(in Matrix2 matrix, in Vector2 vec)
        {
            return new Vector2(matrix.M00 * vec.X + matrix.M01 * vec.Y,
                               matrix.M10 * vec.X + matrix.M11 * vec.Y);
        }

        public static bool operator ==(Matrix2 left, Matrix2 right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Matrix2 left, Matrix2 right)
        {
            return !(left == right);
        }
    }
}
