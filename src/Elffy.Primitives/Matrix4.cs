#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Elffy.Mathematics;
using Elffy.Markup;
using RP = Elffy.Markup.RegexPatterns;
using NVec3 = System.Numerics.Vector3;
using NMat4 = System.Numerics.Matrix4x4;
using System.Diagnostics.CodeAnalysis;

namespace Elffy
{
    /// <summary>Matrix of 4x4</summary>
    [StructLayout(LayoutKind.Explicit)]
    [UseLiteralMarkup]
    [LiteralMarkupPattern(LiteralPattern, LiteralEmit)]
    public struct Matrix4 : IEquatable<Matrix4>
    {
        // =================================================
        // [Field Order]
        // Field order is column-major order.
        // 
        // matrix = [M00, M10, M20, M30, M01, M11, M21, M31, M02, M12, M22, M32, M03, M13, M23, M33]
        // | M00 M01 M02 M03 |
        // | M10 M11 M12 M13 |
        // | M20 M21 M22 M23 |
        // | M30 M31 M32 M33 |
        //
        // [Mathmatical Order]
        // Mathmatical order is column-major order.
        // This is popular mathmatical way.
        // 
        // (ex) vector transformation
        // Multiply matrix from forward
        // vec1 = matrix * vec0
        // 
        //  | x1 |   | M00 M01 M02 M03 |   | x0 |
        //  | y1 | = | M10 M11 M12 M13 | * | y0 |
        //  | z1 |   | M20 M21 M22 M23 |   | z0 |
        //  | w1 |   | M30 M31 M32 M33 |   | w0 |
        // 
        //           | M00*x0 + M01*y0 + M02*z0 + M03*w0 |
        //         = | M10*x0 + M11*y0 + M12*z0 + M13*w0 |
        //           | M20*x0 + M21*y0 + M22*z0 + M23*w0 |
        //           | M30*x0 + M31*y0 + M32*z0 + M33*w0 |
        // =================================================

        private const string LiteralPattern =
@$"^(?<m00>{RP.Float}), *(?<m01>{RP.Float}), *(?<m02>{RP.Float}), *(?<m03>{RP.Float}), *(?<m10>{RP.Float}), *(?<m11>{RP.Float}), *(?<m12>{RP.Float}), *(?<m13>{RP.Float}), *(?<m20>{RP.Float}), *(?<m21>{RP.Float}), *(?<m22>{RP.Float}), *(?<m23>{RP.Float}), *(?<m30>{RP.Float}), *(?<m31>{RP.Float}), *(?<m32>{RP.Float}), *(?<m33>{RP.Float})$";
        private const string LiteralEmit =
@"new global::Elffy.Matrix4((float)(${m00}), (float)(${m01}), (float)(${m02}), (float)(${m03}), (float)(${m10}), (float)(${m11}), (float)(${m12}), (float)(${m13}), (float)(${m20}), (float)(${m21}), (float)(${m22}), (float)(${m23}), (float)(${m30}), (float)(${m31}), (float)(${m32}), (float)(${m33}))";

        [FieldOffset(0)]
        public float M00;
        [FieldOffset(4)]
        public float M10;
        [FieldOffset(8)]
        public float M20;
        [FieldOffset(12)]
        public float M30;

        [FieldOffset(16)]
        public float M01;
        [FieldOffset(20)]
        public float M11;
        [FieldOffset(24)]
        public float M21;
        [FieldOffset(28)]
        public float M31;

        [FieldOffset(32)]
        public float M02;
        [FieldOffset(36)]
        public float M12;
        [FieldOffset(40)]
        public float M22;
        [FieldOffset(44)]
        public float M32;

        [FieldOffset(48)]
        public float M03;
        [FieldOffset(52)]
        public float M13;
        [FieldOffset(56)]
        public float M23;
        [FieldOffset(60)]
        public float M33;

        public ref Vector4 Column0
        {
            [UnscopedRef]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref Unsafe.As<float, Vector4>(ref M00);
        }

        public ref Vector4 Column1
        {
            [UnscopedRef]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref Unsafe.As<float, Vector4>(ref M01);
        }

        public ref Vector4 Column2
        {
            [UnscopedRef]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref Unsafe.As<float, Vector4>(ref M02);
        }

        public ref Vector4 Column3
        {
            [UnscopedRef]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref Unsafe.As<float, Vector4>(ref M03);
        }

        public static unsafe readonly int SizeInBytes = sizeof(Matrix4);

        [LiteralMarkupMember]
        public static readonly Matrix4 Identity = new Matrix4(1, 0, 0, 0,
                                                              0, 1, 0, 0,
                                                              0, 0, 1, 0,
                                                              0, 0, 0, 1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Matrix4(float m00, float m01, float m02, float m03,
                       float m10, float m11, float m12, float m13,
                       float m20, float m21, float m22, float m23,
                       float m30, float m31, float m32, float m33)
        {
            M00 = m00;
            M10 = m10;
            M20 = m20;
            M30 = m30;
            M01 = m01;
            M11 = m11;
            M21 = m21;
            M31 = m31;
            M02 = m02;
            M12 = m12;
            M22 = m22;
            M32 = m32;
            M03 = m03;
            M13 = m13;
            M23 = m23;
            M33 = m33;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Matrix4(in Vector4 col0, in Vector4 col1, in Vector4 col2, in Vector4 col3)
        {
            Column0 = col0;
            Column1 = col1;
            Column2 = col2;
            Column3 = col3;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Matrix4(ReadOnlySpan<Vector4> columns)
        {
            if(columns.Length != 4) { throw new ArgumentException("Length must be 4."); }
            this = Unsafe.As<Vector4, Matrix4>(ref MemoryMarshal.GetReference(columns));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Matrix4(ReadOnlySpan<float> matrix)
        {
            if(matrix.Length != 16) { throw new ArgumentException("Length must be 16."); }
            this = Unsafe.As<float, Matrix4>(ref MemoryMarshal.GetReference(matrix));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Matrix4(in Matrix3 matrix) : this(matrix.M00, matrix.M01, matrix.M02, 0,
                                                   matrix.M10, matrix.M11, matrix.M12, 0,
                                                   matrix.M20, matrix.M21, matrix.M22, 0,
                                                   0, 0, 0, 1)
        { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Transpose() => (M10, M20, M30, M01, M21, M31, M02, M12, M32, M03, M13, M23) = (M01, M02, M03, M10, M12, M13, M20, M21, M23, M30, M31, M32);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Matrix4 Transposed() => new Matrix4(M00, M10, M20, M30,
                                                            M01, M11, M21, M31,
                                                            M02, M12, M22, M32,
                                                            M03, M13, M23, M33);
        public readonly Matrix4 Inverted() => Inverted(out var inv) ? inv : default;

        public readonly bool Inverted(out Matrix4 result)
        {
            Unsafe.SkipInit(out result);
            return NMat4.Invert(AsNMat4(this), out Unsafe.As<Matrix4, NMat4>(ref result));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object? obj) => obj is Matrix4 matrix && Equals(matrix);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Matrix4 other) => AsNMat4(this) == AsNMat4(other);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.AddBytes(this.AsReadOnlyBytes());
            return hash.ToHashCode();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly override string ToString()
        {
            return $"|{M00}, {M01}, {M02}, {M03}|{Environment.NewLine}|{M10}, {M11}, {M12}, {M13}|{Environment.NewLine}|{M20}, {M21}, {M22}, {M23}|{Environment.NewLine}|{M30}, {M31}, {M32}, {M33}|";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 operator *(in Matrix4 matrix, in Vector4 vec)
            => new Vector4(Row0(matrix).Dot(vec), Row1(matrix).Dot(vec), Row2(matrix).Dot(vec), Row3(matrix).Dot(vec));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix4 operator *(in Matrix4 m1, in Matrix4 m2)
        {
            return AsMatrix4(
                NMat4.Multiply(AsNMat4(m2), AsNMat4(m1))
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(in Matrix4 left, in Matrix4 right) => left.Equals(right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(in Matrix4 left, in Matrix4 right) => !(left == right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void OrthographicProjection(float left, float right, float bottom, float top, float depthNear, float depthFar, out Matrix4 result)
        {
            var invRL = 1.0f / (right - left);
            var invTB = 1.0f / (top - bottom);
            var invFN = 1.0f / (depthFar - depthNear);

            result = new Matrix4(2 * invRL, 0, 0, -(right + left) * invRL,
                                 0, 2 * invTB, 0, -(top + bottom) * invTB,
                                 0, 0, -2 * invFN, -(depthFar + depthNear) * invFN,
                                 0, 0, 0, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PerspectiveProjection(float left, float right, float bottom, float top, float depthNear, float depthFar, out Matrix4 result)
        {
            if(depthNear <= 0) { throw new ArgumentOutOfRangeException(nameof(depthNear)); }
            if(depthFar <= 0) { throw new ArgumentOutOfRangeException(nameof(depthFar)); }
            if(depthNear >= depthFar) { throw new ArgumentOutOfRangeException(nameof(depthNear)); }

            var x = 2.0f * depthNear / (right - left);
            var y = 2.0f * depthNear / (top - bottom);
            var a = (right + left) / (right - left);
            var b = (top + bottom) / (top - bottom);
            var c = -(depthFar + depthNear) / (depthFar - depthNear);
            var d = -(2.0f * depthFar * depthNear) / (depthFar - depthNear);

            result = new Matrix4(x, 0, a, 0,
                                 0, y, b, 0,
                                 0, 0, c, d,
                                 0, 0, -1, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PerspectiveProjection(float fovy, float aspect, float depthNear, float depthFar, out Matrix4 result)
        {
            if(fovy <= 0 || fovy > MathTool.Pi) { throw new ArgumentOutOfRangeException(nameof(fovy)); }
            if(aspect <= 0) { throw new ArgumentOutOfRangeException(nameof(aspect)); }
            if(depthNear <= 0) { throw new ArgumentOutOfRangeException(nameof(depthNear)); }
            if(depthFar <= 0) { throw new ArgumentOutOfRangeException(nameof(depthFar)); }

            var maxY = depthNear * MathF.Tan(0.5f * fovy);
            var minY = -maxY;
            var minX = minY * aspect;
            var maxX = maxY * aspect;

            PerspectiveProjection(minX, maxX, minY, maxY, depthNear, depthFar, out result);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LookAt(in Vector3 eye, in Vector3 target, in Vector3 up, out Matrix4 result)
        {
            var z = (eye - target).Normalized();
            var x = up.Cross(z).Normalized();
            var y = z.Cross(x).Normalized();
            result = new Matrix4(x.X, x.Y, x.Z, -x.Dot(eye),
                                 y.X, y.Y, y.Z, -y.Dot(eye),
                                 z.X, z.Y, z.Z, -z.Dot(eye),
                                 0, 0, 0, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void FromAxisAngle(in Vector3 axis, float angle, out Matrix4 result)
        {
            result = AsMatrix4(NMat4.CreateFromAxisAngle(AsNVec3(axis), angle));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix4 FromAxisAngle(in Vector3 axis, float angle)
        {
            FromAxisAngle(axis, angle, out var result);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix4 FromScaleAndTranslation(in Vector3 scale, in Vector3 translation)
        {
            return new Matrix4(
                scale.X, 0, 0, translation.X,
                0, scale.Y, 0, translation.Y,
                0, 0, scale.Z, translation.Z,
                0, 0, 0, 1
            );
        }

        public static ref readonly Vector4 Col0(in Matrix4 matrix) => ref Unsafe.As<float, Vector4>(ref Unsafe.AsRef(matrix.M00));
        public static ref readonly Vector4 Col1(in Matrix4 matrix) => ref Unsafe.As<float, Vector4>(ref Unsafe.AsRef(matrix.M01));
        public static ref readonly Vector4 Col2(in Matrix4 matrix) => ref Unsafe.As<float, Vector4>(ref Unsafe.AsRef(matrix.M02));
        public static ref readonly Vector4 Col3(in Matrix4 matrix) => ref Unsafe.As<float, Vector4>(ref Unsafe.AsRef(matrix.M03));
        public static Vector4 Row0(in Matrix4 matrix) => new Vector4(matrix.M00, matrix.M01, matrix.M02, matrix.M03);
        public static Vector4 Row1(in Matrix4 matrix) => new Vector4(matrix.M10, matrix.M11, matrix.M12, matrix.M13);
        public static Vector4 Row2(in Matrix4 matrix) => new Vector4(matrix.M20, matrix.M21, matrix.M22, matrix.M23);
        public static Vector4 Row3(in Matrix4 matrix) => new Vector4(matrix.M30, matrix.M31, matrix.M32, matrix.M33);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ref readonly NMat4 AsNMat4(in Matrix4 m) => ref Unsafe.As<Matrix4, NMat4>(ref Unsafe.AsRef(m));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ref readonly Matrix4 AsMatrix4(in NMat4 m) => ref Unsafe.As<NMat4, Matrix4>(ref Unsafe.AsRef(m));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ref readonly NVec3 AsNVec3(in Vector3 vec) => ref Unsafe.As<Vector3, NVec3>(ref Unsafe.AsRef(vec));
    }

    public static class MatrixExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<byte> AsBytes(ref this Matrix4 matrix) => MemoryMarshal.CreateSpan(ref Unsafe.As<Matrix4, byte>(ref matrix), Unsafe.SizeOf<Matrix4>());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<byte> AsReadOnlyBytes(in this Matrix4 matrix)
            => MemoryMarshal.CreateSpan(ref Unsafe.As<Matrix4, byte>(ref Unsafe.AsRef(in matrix)), Unsafe.SizeOf<Matrix4>());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<float> AsFloatSpan(ref this Matrix4 matrix) => MemoryMarshal.CreateSpan(ref matrix.M00, 16);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<float> AsReadOnlyFloatSpan(in this Matrix4 matrix) => MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in matrix.M00), 16);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<Vector4> AsVec4Span(ref this Matrix4 matrix) => MemoryMarshal.CreateSpan(ref Unsafe.As<Matrix4, Vector4>(ref matrix), 4);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<Vector4> AsReadOnlyVec4Span(in this Matrix4 matrix)
            => MemoryMarshal.CreateSpan(ref Unsafe.As<Matrix4, Vector4>(ref Unsafe.AsRef(in matrix)), 4);
    }
}
