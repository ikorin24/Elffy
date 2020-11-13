#nullable enable
using System;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Runtime.CompilerServices;
using Elffy.Effective;
using Matrix4x4 = System.Numerics.Matrix4x4;
using N_Vector4 = System.Numerics.Vector4;

namespace Elffy
{
    internal static class MatrixHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Invert(in Matrix4 matrix, out Matrix4 result)
        {
            // =====================================================================
            // Following code is based on System.Numerics.Matrix4x4.Invert .
            // https://github.com/dotnet/runtime/blob/e64bc548c609455652fcd4107f1f4a2ac3084ff3/src/libraries/System.Private.CoreLib/src/System/Numerics/Matrix4x4.cs#L1330
            // 
            // 
            // --------------------------------------------------------------------
            //                        || Elffy.Matrix4 | System.Numerics.Matrix4x4 |
            // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - 
            //      memory layout     || column major  |        row major          |
            // the way of multiplying || from forward  |      from backword        |
            // --------------------------------------------------------------------
            //
            // Therefore, I can use the same code as System.Numerics.Matrix4x4.
            //
            // =====================================================================


            // This implementation is based on the DirectX Math Library XMMatrixInverse method
            // https://github.com/microsoft/DirectXMath/blob/master/Inc/DirectXMathMatrix.inl

            Unsafe.SkipInit(out result);    // The variable must be set before Unsafe.As, but it is set as 'out' parameter. So, skip initialization.

            if(Sse.IsSupported) {
                //return SseImpl(matrix, out result);
                return SseImpl(UnsafeEx.As<Matrix4, Matrix4x4>(in matrix), out Unsafe.As<Matrix4, Matrix4x4>(ref result));
            }

            return SoftwareFallback(UnsafeEx.As<Matrix4, Matrix4x4>(in matrix), out Unsafe.As<Matrix4, Matrix4x4>(ref result));

            static unsafe bool SseImpl(Matrix4x4 matrix, out Matrix4x4 result)
            {
                if(!Sse.IsSupported) {
                    // Redundant test so we won't prejit remainder of this method on platforms without SSE.
                    throw new PlatformNotSupportedException();
                }

                // Load the matrix values into rows
                Vector128<float> row1 = Sse.LoadVector128(&matrix.M11);
                Vector128<float> row2 = Sse.LoadVector128(&matrix.M21);
                Vector128<float> row3 = Sse.LoadVector128(&matrix.M31);
                Vector128<float> row4 = Sse.LoadVector128(&matrix.M41);

                // Transpose the matrix
                Vector128<float> vTemp1 = Sse.Shuffle(row1, row2, 0x44); //_MM_SHUFFLE(1, 0, 1, 0)
                Vector128<float> vTemp3 = Sse.Shuffle(row1, row2, 0xEE); //_MM_SHUFFLE(3, 2, 3, 2)
                Vector128<float> vTemp2 = Sse.Shuffle(row3, row4, 0x44); //_MM_SHUFFLE(1, 0, 1, 0)
                Vector128<float> vTemp4 = Sse.Shuffle(row3, row4, 0xEE); //_MM_SHUFFLE(3, 2, 3, 2)

                row1 = Sse.Shuffle(vTemp1, vTemp2, 0x88); //_MM_SHUFFLE(2, 0, 2, 0)
                row2 = Sse.Shuffle(vTemp1, vTemp2, 0xDD); //_MM_SHUFFLE(3, 1, 3, 1)
                row3 = Sse.Shuffle(vTemp3, vTemp4, 0x88); //_MM_SHUFFLE(2, 0, 2, 0)
                row4 = Sse.Shuffle(vTemp3, vTemp4, 0xDD); //_MM_SHUFFLE(3, 1, 3, 1)

                Vector128<float> V00 = Permute(row3, 0x50);           //_MM_SHUFFLE(1, 1, 0, 0)
                Vector128<float> V10 = Permute(row4, 0xEE);           //_MM_SHUFFLE(3, 2, 3, 2)
                Vector128<float> V01 = Permute(row1, 0x50);           //_MM_SHUFFLE(1, 1, 0, 0)
                Vector128<float> V11 = Permute(row2, 0xEE);           //_MM_SHUFFLE(3, 2, 3, 2)
                Vector128<float> V02 = Sse.Shuffle(row3, row1, 0x88); //_MM_SHUFFLE(2, 0, 2, 0)
                Vector128<float> V12 = Sse.Shuffle(row4, row2, 0xDD); //_MM_SHUFFLE(3, 1, 3, 1)

                Vector128<float> D0 = Sse.Multiply(V00, V10);
                Vector128<float> D1 = Sse.Multiply(V01, V11);
                Vector128<float> D2 = Sse.Multiply(V02, V12);

                V00 = Permute(row3, 0xEE);           //_MM_SHUFFLE(3, 2, 3, 2)
                V10 = Permute(row4, 0x50);           //_MM_SHUFFLE(1, 1, 0, 0)
                V01 = Permute(row1, 0xEE);           //_MM_SHUFFLE(3, 2, 3, 2)
                V11 = Permute(row2, 0x50);           //_MM_SHUFFLE(1, 1, 0, 0)
                V02 = Sse.Shuffle(row3, row1, 0xDD); //_MM_SHUFFLE(3, 1, 3, 1)
                V12 = Sse.Shuffle(row4, row2, 0x88); //_MM_SHUFFLE(2, 0, 2, 0)

                // Note:  We use this expansion pattern instead of Fused Multiply Add
                // in order to support older hardware
                D0 = Sse.Subtract(D0, Sse.Multiply(V00, V10));
                D1 = Sse.Subtract(D1, Sse.Multiply(V01, V11));
                D2 = Sse.Subtract(D2, Sse.Multiply(V02, V12));

                // V11 = D0Y,D0W,D2Y,D2Y
                V11 = Sse.Shuffle(D0, D2, 0x5D);  //_MM_SHUFFLE(1, 1, 3, 1)
                V00 = Permute(row2, 0x49);        //_MM_SHUFFLE(1, 0, 2, 1)
                V10 = Sse.Shuffle(V11, D0, 0x32); //_MM_SHUFFLE(0, 3, 0, 2)
                V01 = Permute(row1, 0x12);        //_MM_SHUFFLE(0, 1, 0, 2)
                V11 = Sse.Shuffle(V11, D0, 0x99); //_MM_SHUFFLE(2, 1, 2, 1)

                // V13 = D1Y,D1W,D2W,D2W
                Vector128<float> V13 = Sse.Shuffle(D1, D2, 0xFD); //_MM_SHUFFLE(3, 3, 3, 1)
                V02 = Permute(row4, 0x49);                        //_MM_SHUFFLE(1, 0, 2, 1)
                V12 = Sse.Shuffle(V13, D1, 0x32);                 //_MM_SHUFFLE(0, 3, 0, 2)
                Vector128<float> V03 = Permute(row3, 0x12);       //_MM_SHUFFLE(0, 1, 0, 2)
                V13 = Sse.Shuffle(V13, D1, 0x99);                 //_MM_SHUFFLE(2, 1, 2, 1)

                Vector128<float> C0 = Sse.Multiply(V00, V10);
                Vector128<float> C2 = Sse.Multiply(V01, V11);
                Vector128<float> C4 = Sse.Multiply(V02, V12);
                Vector128<float> C6 = Sse.Multiply(V03, V13);

                // V11 = D0X,D0Y,D2X,D2X
                V11 = Sse.Shuffle(D0, D2, 0x4);    //_MM_SHUFFLE(0, 0, 1, 0)
                V00 = Permute(row2, 0x9e);         //_MM_SHUFFLE(2, 1, 3, 2)
                V10 = Sse.Shuffle(D0, V11, 0x93);  //_MM_SHUFFLE(2, 1, 0, 3)
                V01 = Permute(row1, 0x7b);         //_MM_SHUFFLE(1, 3, 2, 3)
                V11 = Sse.Shuffle(D0, V11, 0x26);  //_MM_SHUFFLE(0, 2, 1, 2)

                // V13 = D1X,D1Y,D2Z,D2Z
                V13 = Sse.Shuffle(D1, D2, 0xa4);   //_MM_SHUFFLE(2, 2, 1, 0)
                V02 = Permute(row4, 0x9e);         //_MM_SHUFFLE(2, 1, 3, 2)
                V12 = Sse.Shuffle(D1, V13, 0x93);  //_MM_SHUFFLE(2, 1, 0, 3)
                V03 = Permute(row3, 0x7b);         //_MM_SHUFFLE(1, 3, 2, 3)
                V13 = Sse.Shuffle(D1, V13, 0x26);  //_MM_SHUFFLE(0, 2, 1, 2)

                C0 = Sse.Subtract(C0, Sse.Multiply(V00, V10));
                C2 = Sse.Subtract(C2, Sse.Multiply(V01, V11));
                C4 = Sse.Subtract(C4, Sse.Multiply(V02, V12));
                C6 = Sse.Subtract(C6, Sse.Multiply(V03, V13));

                V00 = Permute(row2, 0x33); //_MM_SHUFFLE(0, 3, 0, 3)

                // V10 = D0Z,D0Z,D2X,D2Y
                V10 = Sse.Shuffle(D0, D2, 0x4A); //_MM_SHUFFLE(1, 0, 2, 2)
                V10 = Permute(V10, 0x2C);        //_MM_SHUFFLE(0, 2, 3, 0)
                V01 = Permute(row1, 0x8D);       //_MM_SHUFFLE(2, 0, 3, 1)

                // V11 = D0X,D0W,D2X,D2Y
                V11 = Sse.Shuffle(D0, D2, 0x4C); //_MM_SHUFFLE(1, 0, 3, 0)
                V11 = Permute(V11, 0x93);        //_MM_SHUFFLE(2, 1, 0, 3)
                V02 = Permute(row4, 0x33);       //_MM_SHUFFLE(0, 3, 0, 3)

                // V12 = D1Z,D1Z,D2Z,D2W
                V12 = Sse.Shuffle(D1, D2, 0xEA); //_MM_SHUFFLE(3, 2, 2, 2)
                V12 = Permute(V12, 0x2C);        //_MM_SHUFFLE(0, 2, 3, 0)
                V03 = Permute(row3, 0x8D);       //_MM_SHUFFLE(2, 0, 3, 1)

                // V13 = D1X,D1W,D2Z,D2W
                V13 = Sse.Shuffle(D1, D2, 0xEC); //_MM_SHUFFLE(3, 2, 3, 0)
                V13 = Permute(V13, 0x93);        //_MM_SHUFFLE(2, 1, 0, 3)

                V00 = Sse.Multiply(V00, V10);
                V01 = Sse.Multiply(V01, V11);
                V02 = Sse.Multiply(V02, V12);
                V03 = Sse.Multiply(V03, V13);

                Vector128<float> C1 = Sse.Subtract(C0, V00);
                C0 = Sse.Add(C0, V00);
                Vector128<float> C3 = Sse.Add(C2, V01);
                C2 = Sse.Subtract(C2, V01);
                Vector128<float> C5 = Sse.Subtract(C4, V02);
                C4 = Sse.Add(C4, V02);
                Vector128<float> C7 = Sse.Add(C6, V03);
                C6 = Sse.Subtract(C6, V03);

                C0 = Sse.Shuffle(C0, C1, 0xD8); //_MM_SHUFFLE(3, 1, 2, 0)
                C2 = Sse.Shuffle(C2, C3, 0xD8); //_MM_SHUFFLE(3, 1, 2, 0)
                C4 = Sse.Shuffle(C4, C5, 0xD8); //_MM_SHUFFLE(3, 1, 2, 0)
                C6 = Sse.Shuffle(C6, C7, 0xD8); //_MM_SHUFFLE(3, 1, 2, 0)

                C0 = Permute(C0, 0xD8); //_MM_SHUFFLE(3, 1, 2, 0)
                C2 = Permute(C2, 0xD8); //_MM_SHUFFLE(3, 1, 2, 0)
                C4 = Permute(C4, 0xD8); //_MM_SHUFFLE(3, 1, 2, 0)
                C6 = Permute(C6, 0xD8); //_MM_SHUFFLE(3, 1, 2, 0)

                // Get the determinant
                vTemp2 = row1;
                float det = N_Vector4.Dot(C0.AsVector4(), vTemp2.AsVector4());

                // Check determinate is not zero
                if(MathF.Abs(det) < float.Epsilon) {
                    result = new Matrix4x4(float.NaN, float.NaN, float.NaN, float.NaN,
                                float.NaN, float.NaN, float.NaN, float.NaN,
                                float.NaN, float.NaN, float.NaN, float.NaN,
                                float.NaN, float.NaN, float.NaN, float.NaN);
                    return false;
                }

                // Create Vector128<float> copy of the determinant and invert them.
                Vector128<float> ones = Vector128.Create(1.0f);
                Vector128<float> vTemp = Vector128.Create(det);
                vTemp = Sse.Divide(ones, vTemp);

                row1 = Sse.Multiply(C0, vTemp);
                row2 = Sse.Multiply(C2, vTemp);
                row3 = Sse.Multiply(C4, vTemp);
                row4 = Sse.Multiply(C6, vTemp);

                Unsafe.SkipInit(out result);
                ref Vector128<float> vResult = ref Unsafe.As<Matrix4x4, Vector128<float>>(ref result);

                vResult = row1;
                Unsafe.Add(ref vResult, 1) = row2;
                Unsafe.Add(ref vResult, 2) = row3;
                Unsafe.Add(ref vResult, 3) = row4;

                return true;
            }

            static bool SoftwareFallback(in Matrix4x4 matrix, out Matrix4x4 result)
            {
                //                                       -1
                // If you have matrix M, inverse Matrix M   can compute
                //
                //     -1       1
                //    M   = --------- A
                //            det(M)
                //
                // A is adjugate (adjoint) of M, where,
                //
                //      T
                // A = C
                //
                // C is Cofactor matrix of M, where,
                //           i + j
                // C   = (-1)      * det(M  )
                //  ij                    ij
                //
                //     [ a b c d ]
                // M = [ e f g h ]
                //     [ i j k l ]
                //     [ m n o p ]
                //
                // First Row
                //           2 | f g h |
                // C   = (-1)  | j k l | = + ( f ( kp - lo ) - g ( jp - ln ) + h ( jo - kn ) )
                //  11         | n o p |
                //
                //           3 | e g h |
                // C   = (-1)  | i k l | = - ( e ( kp - lo ) - g ( ip - lm ) + h ( io - km ) )
                //  12         | m o p |
                //
                //           4 | e f h |
                // C   = (-1)  | i j l | = + ( e ( jp - ln ) - f ( ip - lm ) + h ( in - jm ) )
                //  13         | m n p |
                //
                //           5 | e f g |
                // C   = (-1)  | i j k | = - ( e ( jo - kn ) - f ( io - km ) + g ( in - jm ) )
                //  14         | m n o |
                //
                // Second Row
                //           3 | b c d |
                // C   = (-1)  | j k l | = - ( b ( kp - lo ) - c ( jp - ln ) + d ( jo - kn ) )
                //  21         | n o p |
                //
                //           4 | a c d |
                // C   = (-1)  | i k l | = + ( a ( kp - lo ) - c ( ip - lm ) + d ( io - km ) )
                //  22         | m o p |
                //
                //           5 | a b d |
                // C   = (-1)  | i j l | = - ( a ( jp - ln ) - b ( ip - lm ) + d ( in - jm ) )
                //  23         | m n p |
                //
                //           6 | a b c |
                // C   = (-1)  | i j k | = + ( a ( jo - kn ) - b ( io - km ) + c ( in - jm ) )
                //  24         | m n o |
                //
                // Third Row
                //           4 | b c d |
                // C   = (-1)  | f g h | = + ( b ( gp - ho ) - c ( fp - hn ) + d ( fo - gn ) )
                //  31         | n o p |
                //
                //           5 | a c d |
                // C   = (-1)  | e g h | = - ( a ( gp - ho ) - c ( ep - hm ) + d ( eo - gm ) )
                //  32         | m o p |
                //
                //           6 | a b d |
                // C   = (-1)  | e f h | = + ( a ( fp - hn ) - b ( ep - hm ) + d ( en - fm ) )
                //  33         | m n p |
                //
                //           7 | a b c |
                // C   = (-1)  | e f g | = - ( a ( fo - gn ) - b ( eo - gm ) + c ( en - fm ) )
                //  34         | m n o |
                //
                // Fourth Row
                //           5 | b c d |
                // C   = (-1)  | f g h | = - ( b ( gl - hk ) - c ( fl - hj ) + d ( fk - gj ) )
                //  41         | j k l |
                //
                //           6 | a c d |
                // C   = (-1)  | e g h | = + ( a ( gl - hk ) - c ( el - hi ) + d ( ek - gi ) )
                //  42         | i k l |
                //
                //           7 | a b d |
                // C   = (-1)  | e f h | = - ( a ( fl - hj ) - b ( el - hi ) + d ( ej - fi ) )
                //  43         | i j l |
                //
                //           8 | a b c |
                // C   = (-1)  | e f g | = + ( a ( fk - gj ) - b ( ek - gi ) + c ( ej - fi ) )
                //  44         | i j k |
                //
                // Cost of operation
                // 53 adds, 104 muls, and 1 div.
                float a = matrix.M11, b = matrix.M12, c = matrix.M13, d = matrix.M14;
                float e = matrix.M21, f = matrix.M22, g = matrix.M23, h = matrix.M24;
                float i = matrix.M31, j = matrix.M32, k = matrix.M33, l = matrix.M34;
                float m = matrix.M41, n = matrix.M42, o = matrix.M43, p = matrix.M44;

                float kp_lo = k * p - l * o;
                float jp_ln = j * p - l * n;
                float jo_kn = j * o - k * n;
                float ip_lm = i * p - l * m;
                float io_km = i * o - k * m;
                float in_jm = i * n - j * m;

                float a11 = +(f * kp_lo - g * jp_ln + h * jo_kn);
                float a12 = -(e * kp_lo - g * ip_lm + h * io_km);
                float a13 = +(e * jp_ln - f * ip_lm + h * in_jm);
                float a14 = -(e * jo_kn - f * io_km + g * in_jm);

                float det = a * a11 + b * a12 + c * a13 + d * a14;

                if(MathF.Abs(det) < float.Epsilon) {
                    result = new Matrix4x4(float.NaN, float.NaN, float.NaN, float.NaN,
                                           float.NaN, float.NaN, float.NaN, float.NaN,
                                           float.NaN, float.NaN, float.NaN, float.NaN,
                                           float.NaN, float.NaN, float.NaN, float.NaN);
                    return false;
                }

                float invDet = 1.0f / det;

                result.M11 = a11 * invDet;
                result.M21 = a12 * invDet;
                result.M31 = a13 * invDet;
                result.M41 = a14 * invDet;

                result.M12 = -(b * kp_lo - c * jp_ln + d * jo_kn) * invDet;
                result.M22 = +(a * kp_lo - c * ip_lm + d * io_km) * invDet;
                result.M32 = -(a * jp_ln - b * ip_lm + d * in_jm) * invDet;
                result.M42 = +(a * jo_kn - b * io_km + c * in_jm) * invDet;

                float gp_ho = g * p - h * o;
                float fp_hn = f * p - h * n;
                float fo_gn = f * o - g * n;
                float ep_hm = e * p - h * m;
                float eo_gm = e * o - g * m;
                float en_fm = e * n - f * m;

                result.M13 = +(b * gp_ho - c * fp_hn + d * fo_gn) * invDet;
                result.M23 = -(a * gp_ho - c * ep_hm + d * eo_gm) * invDet;
                result.M33 = +(a * fp_hn - b * ep_hm + d * en_fm) * invDet;
                result.M43 = -(a * fo_gn - b * eo_gm + c * en_fm) * invDet;

                float gl_hk = g * l - h * k;
                float fl_hj = f * l - h * j;
                float fk_gj = f * k - g * j;
                float el_hi = e * l - h * i;
                float ek_gi = e * k - g * i;
                float ej_fi = e * j - f * i;

                result.M14 = -(b * gl_hk - c * fl_hj + d * fk_gj) * invDet;
                result.M24 = +(a * gl_hk - c * el_hi + d * ek_gi) * invDet;
                result.M34 = -(a * fl_hj - b * el_hi + d * ej_fi) * invDet;
                result.M44 = +(a * fk_gj - b * ek_gi + c * ej_fi) * invDet;

                return true;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector128<float> Permute(Vector128<float> value, byte control)
        {
            if(Avx.IsSupported) {
                return Avx.Permute(value, control);
            }
            else if(Sse.IsSupported) {
                return Sse.Shuffle(value, value, control);
            }
            else {
                // Redundant test so we won't prejit remainder of this method on platforms without AdvSimd.
                throw new PlatformNotSupportedException();
            }
        }
    }
}
