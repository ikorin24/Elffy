#nullable enable

namespace Elffy.Serialization.Fbx
{
    internal readonly struct Model
    {
        public readonly long ID;
        public readonly Vector3 Translation;
        public readonly Vector3 Rotation;
        public readonly Vector3 Scale;

        public Model(long id, in Vector3 translation, in Vector3 rotation, in Vector3 scale)
        {
            ID = id;
            Translation = translation;
            Rotation = rotation;
            Scale = scale;
        }
    }
}
