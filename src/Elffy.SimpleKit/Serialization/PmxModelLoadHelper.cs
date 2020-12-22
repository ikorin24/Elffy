#nullable enable
using Elffy.Core;
using Elffy.Effective.Unsafes;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using MMD = MMDTools.Unmanaged;

namespace Elffy.Serialization
{
    internal static class PmxModelLoadHelper
    {
        public static unsafe void ReverseTrianglePolygon(Span<MMD.Surface> surfaceList)
        {
            // (a, b, c) を (a, c, b) に書き換える
            fixed(MMD.Surface* s = surfaceList) {
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
        public static Vertex ToVertex(this MMD.Vertex v) => new Vertex(ToVector3(v.Position), ToVector3(v.Normal), ToVector2(v.UV));

        public static RigVertex ToRigVertex(in this MMD.Vertex v)
        {
            // BoneIndex が (A, 0, 0, 0) の時 (A > 0) は
            // Weight が (0, 0, 0, 0) になっているので (1, 0, 0, 0) にする

            //var weight = (v.BoneIndex2 == 0) ? Vector4.UnitX
            //                                 : new Vector4(v.Weight1, v.Weight2, v.Weight3, v.Weight4);

            Vector4 weight;
            if(v.BoneIndex2 == 0) {
                weight = Vector4.UnitX;
            }
            else {
                weight = new Vector4(v.Weight1, v.Weight2, v.Weight3, v.Weight4);
            }

            return new RigVertex(v.Position.ToVector3(),
                                 v.Normal.ToVector3(),
                                 v.UV.AsVector2(),
                                 new Vector4i(v.BoneIndex1, v.BoneIndex2, v.BoneIndex3, v.BoneIndex4),
                                 weight);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Color4 ToColor4(in this MMD.Color color) => color.AsColor4();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref readonly Color4 AsColor4(in this MMD.Color color) => ref UnsafeEx.As<MMD.Color, Color4>(color);

        // Reverse Z because coordinate is inversed
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ToVector3(in this MMD.Vector3 vector) => new Vector3(vector.X, vector.Y, -vector.Z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 ToVector2(in this MMD.Vector2 vector) => vector.AsVector2();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref readonly Vector2 AsVector2(in this MMD.Vector2 vector) => ref UnsafeEx.As<MMD.Vector2, Vector2>(vector);
    }
}
