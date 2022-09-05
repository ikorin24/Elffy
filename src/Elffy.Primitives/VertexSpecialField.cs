#nullable enable

namespace Elffy
{
    public enum VertexSpecialField : byte
    {
        /// <summary>The field layout must be (<see cref="float"/>, <see cref="float"/>, <see cref="float"/>)</summary>
        Position = 0,
        /// <summary>The field layout must be (<see cref="float"/>, <see cref="float"/>)</summary>
        UV,
        /// <summary>The field layout must be (<see cref="float"/>, <see cref="float"/>, <see cref="float"/>)</summary>
        Normal,
        /// <summary>The field layout must be (<see cref="float"/>, <see cref="float"/>, <see cref="float"/>, <see cref="float"/>)</summary>
        Color,
        /// <summary>The field layout must be <see cref="int"/></summary>
        TextureIndex,
        /// <summary>The field layout must be (<see cref="int"/>, <see cref="int"/>, <see cref="int"/>, <see cref="int"/>)</summary>
        Bone,
        /// <summary>The field layout must be (<see cref="float"/>, <see cref="float"/>, <see cref="float"/>, <see cref="float"/>)</summary>
        Weight,
        /// <summary>The field layout must be (<see cref="float"/>, <see cref="float"/>, <see cref="float"/>)</summary>
        Tangent,
        /// <summary>Not specified</summary>
        NotSpecified = 255,
    }
}
