#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TKVector2 = OpenTK.Vector2;
using TKVector3 = OpenTK.Vector3;
using TKVector4 = OpenTK.Vector4;
using TKColor4 = OpenTK.Graphics.Color4;

namespace Elffy.DoNotUse_NowDeveloping         // TODO:
{
    [StructLayout(LayoutKind.Explicit)]
    public struct Vector2
    {
        [FieldOffset(0)]
        public float X;
        [FieldOffset(4)]
        public float Y;

        public static implicit operator TKVector2(Vector2 vec) => Unsafe.As<Vector2, TKVector2>(ref vec);
        public static implicit operator Vector2(TKVector2 vec) => Unsafe.As<TKVector2, Vector2>(ref vec);
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct Vector3
    {
        [FieldOffset(0)]
        public float X;
        [FieldOffset(4)]
        public float Y;
        [FieldOffset(8)]
        public float Z;

        public static implicit operator TKVector3(Vector3 vec) => Unsafe.As<Vector3, TKVector3>(ref vec);
        public static implicit operator Vector3(TKVector3 vec) => Unsafe.As<TKVector3, Vector3>(ref vec);
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct Vector4
    {
        [FieldOffset(0)]
        public float X;
        [FieldOffset(4)]
        public float Y;
        [FieldOffset(8)]
        public float Z;
        [FieldOffset(12)]
        public float W;

        public static implicit operator TKVector4(Vector4 vec) => Unsafe.As<Vector4, TKVector4>(ref vec);
        public static implicit operator Vector4(TKVector4 vec) => Unsafe.As<TKVector4, Vector4>(ref vec);
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct Color4
    {
        [FieldOffset(0)]
        public float R;
        [FieldOffset(4)]
        public float G;
        [FieldOffset(8)]
        public float B;
        [FieldOffset(12)]
        public float A;

        public static implicit operator TKColor4(Color4 color) => Unsafe.As<Color4, TKColor4>(ref color);
        public static implicit operator Color4(TKColor4 color) => Unsafe.As<TKColor4, Color4>(ref color);
    }
}
