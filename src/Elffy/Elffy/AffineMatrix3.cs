#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Elffy
{
    /// <summary>Affine transformation matrix of vector3</summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct AffineMatrix3 : IEquatable<AffineMatrix3>
    {
        // =================================================
        // [Field Order]
        // Field order is column-major order. (but this does not have const field in memory.)
        // 
        // matrix = [M00, M10, M20, M01, M11, M21, M02, M12, M22, X, Y, Z]
        // | M00 M01 M02  X |
        // | M10 M11 M12  Y |
        // | M20 M21 M22  Z |
        // |  0   0   0   1 |
        //
        // [Mathmatical Order]
        // Mathmatical order is column-major order.
        // This is popular mathmatical way.
        // 
        // (ex) vector transformation
        // Multiply matrix from forward
        // vec1 = matrix * vec0
        // 
        //  | x1 |   | M00 M01 M02  X |   | x0 |
        //  | y1 | = | M10 M11 M12  Y | * | y0 |
        //  | z1 |   | M20 M21 M22  Z |   | z0 |
        //  | 1  |   |  0   0   0   1 |   | 1  |
        // 
        //           | M00*x0 + M01*y0 + M02*z0 + X |
        //         = | M10*x0 + M11*y0 + M12*z0 + Y |
        //           | M20*x0 + M21*y0 + M22*z0 + Z |
        // =================================================

        [FieldOffset(0)]
        public float M00;
        [FieldOffset(4)]
        public float M10;
        [FieldOffset(8)]
        public float M20;

        [FieldOffset(12)]
        public float M01;
        [FieldOffset(16)]
        public float M11;
        [FieldOffset(20)]
        public float M21;

        [FieldOffset(24)]
        public float M02;
        [FieldOffset(28)]
        public float M12;
        [FieldOffset(32)]
        public float M22;

        [FieldOffset(36)]
        public float X;
        [FieldOffset(40)]
        public float Y;
        [FieldOffset(44)]
        public float Z;

        public static readonly AffineMatrix3 Identity = new AffineMatrix3(Matrix3.Identity, Vector3.Zero);

        public AffineMatrix3(float m00, float m01, float m02, float x,
                             float m10, float m11, float m12, float y,
                             float m20, float m21, float m22, float z)
        {
            M00 = m00;
            M10 = m10;
            M20 = m20;
            M01 = m01;
            M11 = m11;
            M21 = m21;
            M02 = m02;
            M12 = m12;
            M22 = m22;
            X = x;
            Y = y;
            Z = z;
        }

        public AffineMatrix3(Matrix3 matrix3) : this(matrix3, 0, 0, 0) { }

        public AffineMatrix3(Matrix3 matrix3, float x, float y, float z) : this(matrix3, new Vector3(x, y, z)) { }

        public AffineMatrix3(Matrix3 matrix3, Vector3 translate)
        {
            M00 = matrix3.M00;
            M10 = matrix3.M10;
            M20 = matrix3.M20;
            M01 = matrix3.M01;
            M11 = matrix3.M11;
            M21 = matrix3.M21;
            M02 = matrix3.M02;
            M12 = matrix3.M12;
            M22 = matrix3.M22;
            X = translate.X;
            Y = translate.Y;
            Z = translate.Z;
        }

        public override bool Equals(object? obj) => obj is AffineMatrix3 matrix && Equals(matrix);

        public bool Equals(AffineMatrix3 other)
        {
            return M00 == other.M00 &&
                   M10 == other.M10 &&
                   M20 == other.M20 &&
                   M01 == other.M01 &&
                   M11 == other.M11 &&
                   M21 == other.M21 &&
                   M02 == other.M02 &&
                   M12 == other.M12 &&
                   M22 == other.M22 &&
                   X == other.X &&
                   Y == other.Y &&
                   Z == other.Z;
        }

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(M00);
            hash.Add(M10);
            hash.Add(M20);
            hash.Add(M01);
            hash.Add(M11);
            hash.Add(M21);
            hash.Add(M02);
            hash.Add(M12);
            hash.Add(M22);
            hash.Add(X);
            hash.Add(Y);
            hash.Add(Z);
            return hash.ToHashCode();
        }

        public override string ToString()
        {
            return $"|{M00}, {M01}, {M02}, {X}|{Environment.NewLine}|{M10}, {M11}, {M12}, {Y}|{Environment.NewLine}|{M20}, {M21}, {M22}, {Z}|{Environment.NewLine}|0, 0, 0, 1|";
        }

        public static bool operator ==(AffineMatrix3 left, AffineMatrix3 right) => left.Equals(right);

        public static bool operator !=(AffineMatrix3 left, AffineMatrix3 right) => !(left == right);

        public static AffineMatrix3 operator *(in AffineMatrix3 m1, in AffineMatrix3 m2)
        {
            var m1Row0 = m1.Row0();
            var m1Row1 = m1.Row1();
            var m1Row2 = m1.Row2();
            ref var m2Col0 = ref m2.Col0();
            ref var m2Col1 = ref m2.Col1();
            ref var m2Col2 = ref m2.Col2();
            ref var m2Col3 = ref m2.Col3();

            return new AffineMatrix3((m1Row0 * m2Col0).SumElement(), (m1Row0 * m2Col1).SumElement(), (m1Row0 * m2Col2).SumElement(), (m1Row0 * m2Col3).SumElement() + m1.X,
                                     (m1Row1 * m2Col0).SumElement(), (m1Row1 * m2Col1).SumElement(), (m1Row1 * m2Col2).SumElement(), (m1Row1 * m2Col3).SumElement() + m1.Y,
                                     (m1Row2 * m2Col0).SumElement(), (m1Row2 * m2Col1).SumElement(), (m1Row2 * m2Col2).SumElement(), (m1Row2 * m2Col3).SumElement() + m1.Z);
        }

        public static Vector3 operator *(in AffineMatrix3 m, in Vector3 v) => m.Matrix3() * v + m.Col3();
    }

    internal static class AffineMatrix3Extension
    {
        internal static ref Matrix3 Matrix3(in this AffineMatrix3 matrix) => ref Unsafe.As<AffineMatrix3, Matrix3>(ref Unsafe.AsRef(matrix));
        internal static ref Vector3 Col0(in this AffineMatrix3 matrix) => ref Unsafe.As<float, Vector3>(ref Unsafe.AsRef(matrix.M00));
        internal static ref Vector3 Col1(in this AffineMatrix3 matrix) => ref Unsafe.As<float, Vector3>(ref Unsafe.AsRef(matrix.M01));
        internal static ref Vector3 Col2(in this AffineMatrix3 matrix) => ref Unsafe.As<float, Vector3>(ref Unsafe.AsRef(matrix.M02));
        internal static ref Vector3 Col3(in this AffineMatrix3 matrix) => ref Unsafe.As<float, Vector3>(ref Unsafe.AsRef(matrix.X));

        internal static Vector3 Row0(in this AffineMatrix3 matrix) => new Vector3(matrix.M00, matrix.M01, matrix.M02);
        internal static Vector3 Row1(in this AffineMatrix3 matrix) => new Vector3(matrix.M10, matrix.M11, matrix.M12);
        internal static Vector3 Row2(in this AffineMatrix3 matrix) => new Vector3(matrix.M20, matrix.M21, matrix.M22);
    }
}
