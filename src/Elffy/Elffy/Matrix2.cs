#nullable enable
using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TKVector2 = OpenTK.Vector2;
using TKVector3 = OpenTK.Vector3;
using TKVector4 = OpenTK.Vector4;
using TKColor4 = OpenTK.Graphics.Color4;
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

        // [Mathmatical Order]
        // Mathmatical order is row-major order.
        // This is the reverse of popular mathmatical way.
        // 
        // (ex) vector transformation
        // Multiply matrix from backward
        // vec1 = vec0 * matrix
        //
        // [x1, y1] = [x0, y0] * | M00 M01 |
        //                       | M10 M11 |
        //
        //          = [x0*M00 + y0*M10,   x0*M01 + y0*M11]
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

        public static Matrix2 Rotate(float theta)
        {
            var cos = MathTool.Cos(theta);
            var sin = MathTool.Sin(theta);
            return new Matrix2(cos, sin,
                               -sin, cos);
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

        public static Vector2 operator *(in Vector2 vec, in Matrix2 matrix)
        {
            return new Vector2(matrix.M00 * vec.X + matrix.M10 * vec.Y,
                               matrix.M01 * vec.X + matrix.M11 * vec.Y);
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
