#nullable enable

namespace Elffy.Serialization.Fbx
{
    internal struct VecD3
    {
#pragma warning disable 0649    // The field is never assigned to, and will always have its default value.
        public double X;
        public double Y;
        public double Z;
#pragma warning restore 0649

        public static explicit operator Vector3(in VecD3 vec) => new Vector3((float)vec.X, (float)vec.Y, (float)vec.Z);
    }

    internal struct VecD2
    {
#pragma warning disable 0649    // The field is never assigned to, and will always have its default value.
        public double X;
        public double Y;
#pragma warning restore 0649

        public static explicit operator Vector2(in VecD2 vec) => new Vector2((float)vec.X, (float)vec.Y);
    }
}
