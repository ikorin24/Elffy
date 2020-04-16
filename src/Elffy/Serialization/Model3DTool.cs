#nullable enable
using Elffy.Effective;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Elffy.Serialization
{
    internal static class Model3DTool
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector2 ToVector2(this in MMDTools.Vector2 source) => new Vector2(source.X, source.Y);

        /// <summary>
        /// [NOTE] <see cref="MMDTools.Vector3"/> は DirectX の座標系であるため、Z の値を符号反転して返します。
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector3 ToVector3(this in MMDTools.Vector3 source)
            => new Vector3(source.X, source.Y, -source.Z);                  // NOTE: Reverse sign of Z (because of converting DirectX coordinate to OpenGL)

        /// <summary>三角ポリゴンのサーフェスの表裏を反転させます</summary>
        /// <param name="indexArray">サーフェスを表す頂点番号 (Length は必ず3の倍数)</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ReverseTrianglePolygon(Span<int> indexArray)
        {
            Debug.Assert(indexArray.Length % 3 == 0, $"Length of {nameof(indexArray)} must be divided by three.");
            var surfaces = indexArray.MarshalCast<int, Int32_3>();
            for(int i = 0; i < surfaces.Length; i++) {
                (surfaces[i].Num1, surfaces[i].Num2) = (surfaces[i].Num2, surfaces[i].Num1);
            }
        }

        private struct Int32_3 : IEquatable<Int32_3>
        {
#pragma warning disable 0649    // disable warning "field value is not set"
            internal int Num0;
            internal int Num1;
            internal int Num2;
#pragma warning restore 0649

            public override bool Equals(object? obj) => obj is Int32_3 value && Equals(value);

            public bool Equals(Int32_3 other) => Num0 == other.Num0 && Num1 == other.Num1 && Num2 == other.Num2;

            public override int GetHashCode() => HashCode.Combine(Num0, Num1, Num2);
        }
    }
}
