#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Elffy
{
    /// <summary>Matrix of 2x2</summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct Matrix2 : IEquatable<Matrix2>
    {
        // =================================================
        // [Field Order]
        // Field order is column-major order. (same as opengl)
        // 
        // matrix = [M00, M10, M01, M11]
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
        public float M10;
        [FieldOffset(8)]
        public float M01;
        [FieldOffset(12)]
        public float M11;

        public static readonly Matrix2 Identity = new Matrix2(1, 0, 0, 1);

        /// <summary>
        /// Create new <see cref="Matrix2"/><para/>
        /// [NOTE] Argument order is NOT same as memory layout order !!! (Memory layout is column-major order.)<para/>
        /// </summary>
        /// <param name="m00">element of row=0, col=0</param>
        /// <param name="m01">element of row=0, col=1</param>
        /// <param name="m10">element of row=1, col=0</param>
        /// <param name="m11">element of row=1, col=1</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Matrix2(float m00, float m01, float m10, float m11)
        {
            M00 = m00;
            M01 = m01;
            M10 = m10;
            M11 = m11;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Matrix2(ReadOnlySpan<float> matrix)
        {
            if(matrix.Length < 4) { throw new ArgumentException("Length >= 4 is needed."); }
            M00 = matrix[0];
            M10 = matrix[1];
            M01 = matrix[2];
            M11 = matrix[3];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Transpose()
        {
            var tmp = M01;
            M01 = M10;
            M10 = tmp;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Matrix2 Transposed() => new Matrix2(M00, M10, M01, M11);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix2 GetRotation(float theta)
        {
            var cos = MathF.Cos(theta);
            var sin = MathF.Sin(theta);
            return new Matrix2(cos, -sin,
                               sin, cos);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly override bool Equals(object? obj) => obj is Matrix2 matrix && Equals(matrix);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Equals(Matrix2 other)
        {
            return M00 == other.M00 &&
                   M01 == other.M01 &&
                   M10 == other.M10 &&
                   M11 == other.M11;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly override int GetHashCode() => HashCode.Combine(M00, M01, M10, M11);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly override string ToString() => $"|{M00}, {M01}|{Environment.NewLine}|{M10}, {M11}|";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix2 operator *(in Matrix2 m1, in Matrix2 m2)
            => new Matrix2(m1.M00 * m2.M00 + m1.M01 * m2.M10,    m1.M00 * m2.M01 + m1.M01 * m2.M11,
                           m1.M10 * m2.M00 + m1.M11 * m2.M10,    m1.M10 * m2.M01 + m1.M11 * m2.M11);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator *(in Matrix2 matrix, in Vector2 vec) 
            => new Vector2(matrix.M00 * vec.X + matrix.M01 * vec.Y,
                           matrix.M10 * vec.X + matrix.M11 * vec.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Matrix2 left, Matrix2 right) => left.Equals(right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Matrix2 left, Matrix2 right) => !(left == right);
    }
}
