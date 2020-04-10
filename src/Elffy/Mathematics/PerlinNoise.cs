#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Elffy.Mathematics
{
    //public struct PerlinNoise
    //{

    //    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    //    public static float GetValue(float x)
    //    {
    //        var n = (int)Math.Floor(x);     // 整数部分
    //        var t = x - n;                  // 小数部分
    //        var a0 = GetA(n);
    //        var a1 = GetA(n + 1);
    //        var w0 = Wavelet(a0, t);
    //        var w1 = Wavelet(a1, t - 1f);
    //        var v = w0 * (1 - t) + w1 * t;
    //        return v;
    //    }

    //    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    //    private static float GetA(int n)
    //    {
    //        // 32bit空間での射影
    //        n ^= (n << 13);
    //        n ^= (n >> 17);
    //        n ^= (n << 5);
    //        unchecked {
    //            n += 1234567890;
    //        }

    //        var a = -n / (float)int.MinValue;
    //        return a;
    //        //return 5f;
    //    }

    //    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    //    private static float Wavelet(float a, float t)
    //    {
    //        // -1 <= t <= 1

    //        var t2 = t * t;
    //        var t3 = t2 * t;
    //        var t3abs = (t3 > 0) ? t3 : -t3;

    //        var ret = (1 - 3 * t2 + 2 * t3abs) * a * t;     // (1 - 3t^2 + 2|t^3|) * at
    //        return ret;
    //    }
    //}
}
