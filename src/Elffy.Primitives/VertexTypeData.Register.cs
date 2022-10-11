#nullable enable
using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Elffy;

partial class VertexTypeData
{
    private static readonly object _lockObj = new object();
    private static readonly Hashtable _typeDic = new Hashtable();

    public static bool TryGetVertexTypeData(Type vertexType, [MaybeNullWhen(false)] out VertexTypeData typeData)
    {
        if(vertexType == null) {
            typeData = default;
            return false;
        }
        var data = _typeDic[vertexType];
        Debug.Assert(data is null or VertexTypeData);
        typeData = Unsafe.As<VertexTypeData>(data);
        return typeData != null;
    }

    public static VertexTypeData GetVertexTypeData(Type vertexType)
    {
        if(TryGetVertexTypeData(vertexType, out var typeData) == false) { ThrowTypeNotRegistered(vertexType); }
        return typeData;
    }

    /// <summary>Register a vertex type</summary>
    /// <remarks>[NOTE] This method is intended to be used only from the source generator.</remarks>
    /// <typeparam name="TVertex">vertex type</typeparam>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static VertexTypeData Register<TVertex>(VertexFieldData f1) where TVertex : unmanaged, IVertex
        => RegisterPrivate<TVertex>(new[] { f1 });

    /// <summary>Register a vertex type</summary>
    /// <remarks>[NOTE] This method is intended to be used only from the source generator.</remarks>
    /// <typeparam name="TVertex">vertex type</typeparam>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static VertexTypeData Register<TVertex>(VertexFieldData f1, VertexFieldData f2) where TVertex : unmanaged, IVertex
        => RegisterPrivate<TVertex>(new[] { f1, f2 });

    /// <summary>Register a vertex type</summary>
    /// <remarks>[NOTE] This method is intended to be used only from the source generator.</remarks>
    /// <typeparam name="TVertex">vertex type</typeparam>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static VertexTypeData Register<TVertex>(VertexFieldData f1, VertexFieldData f2, VertexFieldData f3) where TVertex : unmanaged, IVertex
        => RegisterPrivate<TVertex>(new[] { f1, f2, f3 });

    /// <summary>Register a vertex type</summary>
    /// <remarks>[NOTE] This method is intended to be used only from the source generator.</remarks>
    /// <typeparam name="TVertex">vertex type</typeparam>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static VertexTypeData Register<TVertex>(VertexFieldData f1, VertexFieldData f2, VertexFieldData f3, VertexFieldData f4) where TVertex : unmanaged, IVertex
        => RegisterPrivate<TVertex>(new[] { f1, f2, f3, f4 });

    /// <summary>Register a vertex type</summary>
    /// <remarks>[NOTE] This method is intended to be used only from the source generator.</remarks>
    /// <typeparam name="TVertex">vertex type</typeparam>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static VertexTypeData Register<TVertex>(
        VertexFieldData f1, VertexFieldData f2, VertexFieldData f3, VertexFieldData f4,
        VertexFieldData f5) where TVertex : unmanaged, IVertex
        => RegisterPrivate<TVertex>(new[] { f1, f2, f3, f4, f5 });

    /// <summary>Register a vertex type</summary>
    /// <remarks>[NOTE] This method is intended to be used only from the source generator.</remarks>
    /// <typeparam name="TVertex">vertex type</typeparam>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static VertexTypeData Register<TVertex>(
        VertexFieldData f1, VertexFieldData f2, VertexFieldData f3, VertexFieldData f4,
        VertexFieldData f5, VertexFieldData f6) where TVertex : unmanaged, IVertex
        => RegisterPrivate<TVertex>(new[] { f1, f2, f3, f4, f5, f6 });

    /// <summary>Register a vertex type</summary>
    /// <remarks>[NOTE] This method is intended to be used only from the source generator.</remarks>
    /// <typeparam name="TVertex">vertex type</typeparam>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static VertexTypeData Register<TVertex>(
        VertexFieldData f1, VertexFieldData f2, VertexFieldData f3, VertexFieldData f4,
        VertexFieldData f5, VertexFieldData f6, VertexFieldData f7) where TVertex : unmanaged, IVertex
        => RegisterPrivate<TVertex>(new[] { f1, f2, f3, f4, f5, f6, f7 });

    /// <summary>Register a vertex type</summary>
    /// <remarks>[NOTE] This method is intended to be used only from the source generator.</remarks>
    /// <typeparam name="TVertex">vertex type</typeparam>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static VertexTypeData Register<TVertex>(
        VertexFieldData f1, VertexFieldData f2, VertexFieldData f3, VertexFieldData f4,
        VertexFieldData f5, VertexFieldData f6, VertexFieldData f7, VertexFieldData f8) where TVertex : unmanaged, IVertex
        => RegisterPrivate<TVertex>(new[] { f1, f2, f3, f4, f5, f6, f7, f8 });

    /// <summary>Register a vertex type</summary>
    /// <remarks>[NOTE] This method is intended to be used only from the source generator.</remarks>
    /// <typeparam name="TVertex">vertex type</typeparam>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static VertexTypeData Register<TVertex>(
        VertexFieldData f1, VertexFieldData f2, VertexFieldData f3, VertexFieldData f4,
        VertexFieldData f5, VertexFieldData f6, VertexFieldData f7, VertexFieldData f8,
        VertexFieldData f9) where TVertex : unmanaged, IVertex
        => RegisterPrivate<TVertex>(new[] { f1, f2, f3, f4, f5, f6, f7, f8, f9 });

    /// <summary>Register a vertex type</summary>
    /// <remarks>[NOTE] This method is intended to be used only from the source generator.</remarks>
    /// <typeparam name="TVertex">vertex type</typeparam>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static VertexTypeData Register<TVertex>(
        VertexFieldData f1, VertexFieldData f2, VertexFieldData f3, VertexFieldData f4,
        VertexFieldData f5, VertexFieldData f6, VertexFieldData f7, VertexFieldData f8,
        VertexFieldData f9, VertexFieldData f10) where TVertex : unmanaged, IVertex
        => RegisterPrivate<TVertex>(new[] { f1, f2, f3, f4, f5, f6, f7, f8, f9, f10 });

    /// <summary>Register a vertex type</summary>
    /// <remarks>[NOTE] This method is intended to be used only from the source generator.</remarks>
    /// <typeparam name="TVertex">vertex type</typeparam>
    /// <param name="fields">fields data</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static VertexTypeData Register<TVertex>(params VertexFieldData[] fields) where TVertex : unmanaged, IVertex
        => RegisterPrivate<TVertex>(fields.ToArray());

    private static VertexTypeData RegisterPrivate<TVertex>(VertexFieldData[] fields) where TVertex : unmanaged, IVertex
    {
        if(RuntimeHelpers.IsReferenceOrContainsReferences<TVertex>()) {
            throw new ArgumentException($"Vertex type is reference type or contains reference type fields.");
        }
        if(typeof(IVertex).IsAssignableFrom(typeof(TVertex)) == false) {
            throw new ArgumentException($"Vertex type must implement {nameof(IVertex)}. (Type = {typeof(TVertex).FullName})");
        }
        if(VertexAttribute.IsVertexType(typeof(TVertex)) == false) {
            throw new ArgumentException($"Vertex type must has the attribute {nameof(VertexAttribute)} (Type = {typeof(TVertex).FullName})");
        }
        ArgumentNullException.ThrowIfNull(fields);
        foreach(var f in fields) {
            if(f is null) { throw new ArgumentException("Fields data contains null."); }
        }
        var vertexSize = Unsafe.SizeOf<TVertex>();
        var typeData = new VertexTypeData(fields, vertexSize);
        lock(_lockObj) {
            _typeDic.Add(typeof(TVertex), typeData);
        }
        return typeData;
    }

    [DoesNotReturn]
    private static void ThrowTypeNotRegistered(Type vertexType) => throw new ArgumentException($"The vertex type is not registered. (Type = {vertexType.FullName})");
}
