#nullable enable

using FbxTools;

namespace Elffy.Serialization.Fbx
{
    internal readonly struct Model
    {
        public readonly long ID;
        public readonly RawString Name;
        public readonly ModelType Type;
        public readonly Vector3 Translation;
        public readonly Vector3 Rotation;
        public readonly Vector3 Scale;

        public Model(long id, RawString name, ModelType type, in Vector3 translation, in Vector3 rotation, in Vector3 scale)
        {
            ID = id;
            Name = name;
            Type = type;
            Translation = translation;
            Rotation = rotation;
            Scale = scale;
        }
    }
}
