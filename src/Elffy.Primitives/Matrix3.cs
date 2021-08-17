#nullable enable

#if NET5_0_OR_GREATER
#define CAN_SKIP_INIT
#endif

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NVec3 = System.Numerics.Vector3;

namespace Elffy
{
    /// <summary>Matrix of 3x3</summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct Matrix3 : IEquatable<Matrix3>
    {
        // =================================================
        // [Field Order]
        // Field order is column-major order. (same as opengl)
        // 
        // matrix = [M00, M10, M20, M01, M11, M21, M02, M12, M22]
        // | M00 M01 M02 |
        // | M10 M11 M12 |
        // | M20 M21 M22 |
        //
        // [Mathmatical Order]
        // Mathmatical order is column-major order.
        // This is popular mathmatical way.
        // 
        // (ex) vector transformation
        // Multiply matrix from forward
        // vec1 = matrix * vec0
        // 
        //  | x1 |   | M00 M01 M02 |   | x0 |
        //  | y1 | = | M10 M11 M12 | * | y0 |
        //  | z1 |   | M20 M21 M22 |   | z0 |
        //
        //           | M00*x0 + M01*y0 + M02*z0 |
        //         = | M10*x0 + M11*y0 + M12*z0 |
        //           | M20*x0 + M21*y0 + M22*z0 |
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

        public static readonly Matrix3 Identity = new Matrix3(1, 0, 0, 0, 1, 0, 0, 0, 1);

        /// <summary>Get whether the matrix is identity</summary>
        public bool IsIdentity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => M00 == 1 && M11 == 1 && M22 == 1 &&          // Early return if diagonal elements are not 1.
                   M01 == 0 && M02 == 0 && M10 == 0 &&
                   M12 == 0 && M20 == 0 && M21 == 0;
        }

        /// <summary>
        /// Create new <see cref="Matrix3"/><para/>
        /// [NOTE] Argument order is NOT same as memory layout order !!! (Memory layout is column-major order.)<para/>
        /// </summary>
        /// <param name="m00">element of row=0, col=0</param>
        /// <param name="m01">element of row=0, col=1</param>
        /// <param name="m02">element of row=0, col=2</param>
        /// <param name="m10">element of row=1, col=0</param>
        /// <param name="m11">element of row=1, col=1</param>
        /// <param name="m12">element of row=1, col=2</param>
        /// <param name="m20">element of row=2, col=0</param>
        /// <param name="m21">element of row=2, col=1</param>
        /// <param name="m22">element of row=2, col=2</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Matrix3(float m00, float m01, float m02,
                       float m10, float m11, float m12,
                       float m20, float m21, float m22)
        {
            M00 = m00;
            M01 = m01;
            M02 = m02;
            M10 = m10;
            M11 = m11;
            M12 = m12;
            M20 = m20;
            M21 = m21;
            M22 = m22;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Matrix3(in Matrix4 matrix4)
        {
            M00 = matrix4.M00;
            M10 = matrix4.M10;
            M20 = matrix4.M20;
            M01 = matrix4.M01;
            M11 = matrix4.M11;
            M21 = matrix4.M21;
            M02 = matrix4.M02;
            M12 = matrix4.M12;
            M22 = matrix4.M22;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Matrix3(ReadOnlySpan<float> matrix)
        {
            if(matrix.Length < 9) { throw new ArgumentException("Length >= 9 is needed."); }
            M00 = matrix[0];
            M10 = matrix[1];
            M20 = matrix[2];
            M01 = matrix[3];
            M11 = matrix[4];
            M21 = matrix[5];
            M02 = matrix[6];
            M12 = matrix[7];
            M22 = matrix[8];
        }

        public readonly bool Inverted(out Matrix3 result)
        {
            var u = new NVec3(M00, M01, M02) * new NVec3(M11, M12, M10) * new NVec3(M22, M20, M21);
            var v = new NVec3(M02, M01, M00) * new NVec3(M11, M10, M12) * new NVec3(M20, M22, M21);
            var det = 1f / (u.X + u.Y + u.Z - v.X - v.Y - v.Z);
            if(float.IsNaN(det)) {
                result = default;
                return false;
            }

            var x1 = new NVec3(M11, M12, M10);
            var x2 = new NVec3(M22, M20, M21);
            var x3 = new NVec3(M12, M10, M11);
            var x4 = new NVec3(M21, M22, M20);

            var y1 = new NVec3(M02, M00, M01);
            var y2 = new NVec3(M21, M22, M20);
            var y3 = new NVec3(M01, M02, M00);
            var y4 = new NVec3(M22, M20, M21);

            var z1 = new NVec3(M01, M02, M00);
            var z2 = new NVec3(M12, M10, M11);
            var z3 = new NVec3(M02, M00, M01);
            var z4 = new NVec3(M11, M12, M10);

#if CAN_SKIP_INIT
            Unsafe.SkipInit(out result);
            Unsafe.As<float, NVec3>(ref result.M00) = ((x1 * x2) - (x3 * x4)) * det;
            Unsafe.As<float, NVec3>(ref result.M01) = ((y1 * y2) - (y3 * y4)) * det;
            Unsafe.As<float, NVec3>(ref result.M02) = ((z1 * z2) - (z3 * z4)) * det;
#else
            var p = ((x1 * x2) - (x3 * x4)) * det;
            var q = ((y1 * y2) - (y3 * y4)) * det;
            var r = ((z1 * z2) - (z3 * z4)) * det;
            result = new Matrix3(p.X, q.X, r.X,
                                 p.Y, q.Y, r.Y,
                                 p.Z, q.Z, r.Z);
#endif
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Transpose() => (M01, M02, M10, M12, M20, M21) = (M10, M20, M01, M21, M02, M12);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Matrix3 Transposed() => new Matrix3(M00, M10, M20,
                                                            M01, M11, M21,
                                                            M02, M12, M22);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object? obj) => obj is Matrix3 matrix && Equals(matrix);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Equals(Matrix3 other)
        {
            return M00 == other.M00 &&
                   M01 == other.M01 &&
                   M02 == other.M02 &&
                   M10 == other.M10 &&
                   M11 == other.M11 &&
                   M12 == other.M12 &&
                   M20 == other.M20 &&
                   M21 == other.M21 &&
                   M22 == other.M22;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(M00);
            hash.Add(M01);
            hash.Add(M02);
            hash.Add(M10);
            hash.Add(M11);
            hash.Add(M12);
            hash.Add(M20);
            hash.Add(M21);
            hash.Add(M22);
            return hash.ToHashCode();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString()
        {
            return $"|{M00}, {M01}, {M02}|{Environment.NewLine}|{M10}, {M11}, {M12}|{Environment.NewLine}|{M20}, {M21}, {M22}|";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator *(in Matrix3 matrix, in Vector3 vec)
        {
            return Unsafe.As<float, Vector3>(ref Unsafe.AsRef(matrix.M00)) * vec.X +
                   Unsafe.As<float, Vector3>(ref Unsafe.AsRef(matrix.M01)) * vec.Y +
                   Unsafe.As<float, Vector3>(ref Unsafe.AsRef(matrix.M02)) * vec.Z;

            //return new Vector3(matrix.M00 * vec.X + matrix.M01 * vec.Y + matrix.M02 * vec.Z,
            //                   matrix.M10 * vec.X + matrix.M11 * vec.Y + matrix.M12 * vec.Z,
            //                   matrix.M20 * vec.X + matrix.M21 * vec.Y + matrix.M22 * vec.Z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3 operator *(in Matrix3 m1, in Matrix3 m2)
        {
            return new Matrix3(m1.M00 * m2.M00 + m1.M01 * m2.M10 + m1.M02 * m2.M20,   m1.M00 * m2.M01 + m1.M01 * m2.M11 + m1.M02 * m2.M21,   m1.M00 * m2.M02 + m1.M01 * m2.M12 + m1.M02 * m2.M22,
                               m1.M10 * m2.M00 + m1.M11 * m2.M10 + m1.M12 * m2.M20,   m1.M10 * m2.M01 + m1.M11 * m2.M11 + m1.M12 * m2.M21,   m1.M10 * m2.M02 + m1.M11 * m2.M12 + m1.M12 * m2.M22,
                               m1.M20 * m2.M00 + m1.M21 * m2.M10 + m1.M22 * m2.M20,   m1.M20 * m2.M01 + m1.M21 * m2.M11 + m1.M22 * m2.M21,   m1.M20 * m2.M02 + m1.M21 * m2.M12 + m1.M22 * m2.M22);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(in Matrix3 left, in Matrix3 right) => left.Equals(right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(in Matrix3 left, in Matrix3 right) => !(left == right);
    }
}
