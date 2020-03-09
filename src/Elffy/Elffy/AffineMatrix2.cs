#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Elffy
{
    /// <summary>Affine transformation matrix of vector2</summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct AffineMatrix2 : IEquatable<AffineMatrix2>
    {
        // =================================================
        // [Field Order]
        // Field order is column-major order. (but this does not have const field in memory.)
        // 
        // matrix = [M00, M10, M01, M11, X, Y]
        // | M00 M01  X |
        // | M10 M11  Y |
        // |  0   0   1 |
        //
        // [Mathmatical Order]
        // Mathmatical order is column-major order.
        // This is popular mathmatical way.
        // 
        // (ex) vector transformation
        // Multiply matrix from forward
        // vec1 = matrix * vec0
        // 
        //  | x1 |   | M00 M01  X |   | x0 |
        //  | y1 | = | M10 M11  Y | * | y0 |
        //  |(1) |   |  0   0   1 |   |(1) |
        // 
        //           | M00*x0 + M01*y0  + X |
        //         = | M10*x0 + M11*y0  + Y |
        //           |          (1)         |
        // =================================================

        [FieldOffset(0)]
        public float M00;
        [FieldOffset(4)]
        public float M10;

        [FieldOffset(8)]
        public float M01;
        [FieldOffset(12)]
        public float M11;

        [FieldOffset(16)]
        public float X;
        [FieldOffset(20)]
        public float Y;

        public static readonly AffineMatrix2 Identity = new AffineMatrix2(Matrix2.Identity, Vector2.Zero);

        public AffineMatrix2(float m00, float m01, float x, 
                             float m10, float m11, float y)
        {
            M00 = m00;
            M10 = m10;
            M01 = m01;
            M11 = m11;
            X = x;
            Y = y;
        }

        public AffineMatrix2(Matrix2 matrix) : this(matrix, 0, 0) { }

        public AffineMatrix2(Matrix2 matrix, float x, float y) : this(matrix, new Vector2(x, y)) { }

        public AffineMatrix2(Matrix2 matrix, Vector2 translate)
        {
            M00 = matrix.M00;
            M10 = matrix.M10;
            M01 = matrix.M01;
            M11 = matrix.M11;
            X = translate.X;
            Y = translate.Y;
        }

        public override bool Equals(object? obj) => obj is AffineMatrix2 matrix && Equals(matrix);

        public bool Equals(AffineMatrix2 other)
        {
            return M00 == other.M00 &&
                   M10 == other.M10 &&
                   M01 == other.M01 &&
                   M11 == other.M11 &&
                   X == other.X &&
                   Y == other.Y;
        }

        public readonly override int GetHashCode() => HashCode.Combine(M00, M10, M01, M11, X, Y);

        public static bool operator ==(AffineMatrix2 left, AffineMatrix2 right) => left.Equals(right);

        public static bool operator !=(AffineMatrix2 left, AffineMatrix2 right) => !(left == right);

        public override string ToString()
        {
            return $"|{M00}, {M01}, {X}|{Environment.NewLine}|{M10}, {M11}, {Y}|{Environment.NewLine}|0, 0, 1|";
        }

        public static AffineMatrix2 operator *(in AffineMatrix2 m1, in AffineMatrix2 m2)
        {
            var m1Row0 = m1.Row0();
            var m1Row1 = m1.Row1();
            ref var m2Col0 = ref m2.Col0();
            ref var m2Col1 = ref m2.Col1();
            ref var m2Col2 = ref m2.Col2();

            return new AffineMatrix2(m1Row0.Dot(m2Col0), m1Row0.Dot(m2Col1), m1Row0.Dot(m2Col2) + m1.X,
                                     m1Row1.Dot(m2Col0), m1Row1.Dot(m2Col1), m1Row1.Dot(m2Col2) + m1.Y);
        }

        public static Vector2 operator *(in AffineMatrix2 m, in Vector2 v) => m.Matrix2() * v + m.Col2();

        public static explicit operator AffineMatrix2(in Matrix2 matrix) => new AffineMatrix2(matrix);
        public static explicit operator Matrix3(in AffineMatrix2 affine2) => new Matrix3(affine2.M00, affine2.M01, affine2.X,
                                                                                         affine2.M10, affine2.M11, affine2.Y,
                                                                                         0, 0, 1);
    }

    internal static class AffineMatrix2Extension
    {
        internal static ref Matrix2 Matrix2(in this AffineMatrix2 matrix) => ref Unsafe.As<AffineMatrix2, Matrix2>(ref Unsafe.AsRef(matrix));
        internal static ref Vector2 Col0(in this AffineMatrix2 matrix) => ref Unsafe.As<float, Vector2>(ref Unsafe.AsRef(matrix.M00));
        internal static ref Vector2 Col1(in this AffineMatrix2 matrix) => ref Unsafe.As<float, Vector2>(ref Unsafe.AsRef(matrix.M01));
        internal static ref Vector2 Col2(in this AffineMatrix2 matrix) => ref Unsafe.As<float, Vector2>(ref Unsafe.AsRef(matrix.X));

        internal static Vector2 Row0(in this AffineMatrix2 matrix) => new Vector2(matrix.M00, matrix.M01);
        internal static Vector2 Row1(in this AffineMatrix2 matrix) => new Vector2(matrix.M10, matrix.M11);
    }
}
