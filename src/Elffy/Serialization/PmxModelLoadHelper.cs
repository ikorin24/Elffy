#nullable enable
using Elffy.Core;
using System;
using System.Runtime.CompilerServices;

namespace Elffy.Serialization
{
    internal static class PmxModelLoadHelper
    {
        public static unsafe void ReverseTrianglePolygon(Span<MMDTools.Unmanaged.Surface> surfaceList)
        {
            // (a, b, c) を (a, c, b) に書き換える
            fixed(MMDTools.Unmanaged.Surface* s = surfaceList) {
                int* p = (int*)s;
                for(int i = 0; i < surfaceList.Length; i++) {
                    var i1 = i * 3 + 1;
                    var i2 = i * 3 + 2;
                    (p[i1], p[i2]) = (p[i2], p[i1]);
                }
            }
        }

        //private static Material ToMaterial(MMDTools.Material m) => new Material(ToColor4(m.Ambient), ToColor4(m.Diffuse), ToColor4(m.Specular), m.Shininess);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vertex ToVertex(this MMDTools.Unmanaged.Vertex v) => new Vertex(ToVector3(v.Position), ToVector3(v.Normal), ToVector2(v.UV));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RigVertex ToRigVertex(this MMDTools.Unmanaged.Vertex v)
            => new RigVertex(ToVector3(v.Position), ToVector3(v.Normal), ToVector2(v.UV), v.BoneIndex1, v.BoneIndex2, v.BoneIndex3, v.BoneIndex4);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Color4 ToColor4(this MMDTools.Unmanaged.Color color) => Unsafe.As<MMDTools.Unmanaged.Color, Color4>(ref color);

        // Reverse Z because coordinate is inversed
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ToVector3(this MMDTools.Unmanaged.Vector3 vector) => new Vector3(vector.X, vector.Y, -vector.Z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 ToVector2(this MMDTools.Unmanaged.Vector2 vector) => new Vector2(vector.X, vector.Y);
    }
}
