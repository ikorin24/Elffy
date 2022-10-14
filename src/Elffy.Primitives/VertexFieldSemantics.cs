#nullable enable
using System;
using System.Runtime.CompilerServices;

namespace Elffy;

public enum VertexFieldSemantics : byte
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
}

public static class VertexFieldSemanticsExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (int Size, VertexFieldMarshalType MarshalType, int MarshalCount) GetValidLayout(this VertexFieldSemantics semantics)
    {
        if(semantics == VertexFieldSemantics.Position) {
            return (Size: sizeof(float) * 3, MarshalType: VertexFieldMarshalType.Float, MarshalCount: 3);
        }
        if(semantics == VertexFieldSemantics.UV) {
            return (Size: sizeof(float) * 2, MarshalType: VertexFieldMarshalType.Float, MarshalCount: 2);
        }
        if(semantics == VertexFieldSemantics.Normal) {
            return (Size: sizeof(float) * 3, MarshalType: VertexFieldMarshalType.Float, MarshalCount: 3);
        }
        if(semantics == VertexFieldSemantics.Color) {
            return (Size: sizeof(float) * 4, MarshalType: VertexFieldMarshalType.Float, MarshalCount: 4);
        }
        if(semantics == VertexFieldSemantics.TextureIndex) {
            return (Size: sizeof(int) * 1, MarshalType: VertexFieldMarshalType.Int32, MarshalCount: 1);
        }
        if(semantics == VertexFieldSemantics.Bone) {
            return (Size: sizeof(int) * 4, MarshalType: VertexFieldMarshalType.Int32, MarshalCount: 4);
        }
        if(semantics == VertexFieldSemantics.Weight) {
            return (Size: sizeof(float) * 4, MarshalType: VertexFieldMarshalType.Float, MarshalCount: 4);
        }
        if(semantics == VertexFieldSemantics.Tangent) {
            return (Size: sizeof(float) * 3, MarshalType: VertexFieldMarshalType.Float, MarshalCount: 3);
        }
        throw new ArgumentException($"Invalid semantics: {semantics}");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetValidSize(this VertexFieldSemantics semantics)
    {
        return semantics.GetValidLayout().Size;
    }

#if ELFFY_SOURCE_GNENERATOR
    internal static string GetExpectedFieldTypeName(this VertexFieldSemantics semantics) => semantics switch
    {
        VertexFieldSemantics.Position or VertexFieldSemantics.Normal or VertexFieldSemantics.Tangent => "global::Elffy.Vector3",
        VertexFieldSemantics.UV => "global::Elffy.Vector2",
        VertexFieldSemantics.Color or VertexFieldSemantics.Weight => "global::Elffy.Vector4",
        VertexFieldSemantics.TextureIndex => "int",
        VertexFieldSemantics.Bone => "global::Elffy.Vector4i",
        _ => throw new NotImplementedException($"not implemented semantics '{semantics}'"),
    };
#endif
}
