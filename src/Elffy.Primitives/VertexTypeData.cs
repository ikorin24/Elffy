#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Elffy
{
    public sealed class VertexTypeData
    {
        private readonly VertexFieldData[] _fields;
        private readonly int _vertexSize;

        public int VertexSize => _vertexSize;

        public int FieldCount => _fields.Length;

        internal VertexTypeData(VertexFieldData[] fields, int vertexSize)
        {
            _fields = fields;
            _vertexSize = vertexSize;
        }

        public ReadOnlySpan<VertexFieldData> GetFields() => _fields;

        public bool TryGetField(string fieldName, [MaybeNullWhen(false)] out VertexFieldData field)
        {
            ArgumentNullException.ThrowIfNull(fieldName);
            foreach(var f in _fields) {
                if(f.Name == fieldName) {
                    field = f;
                    return true;
                }
            }
            field = null;
            return false;
        }

        public VertexFieldData GetField(string fieldName)
        {
            if(TryGetField(fieldName, out var field) == false) { ThrowFieldNotFound(fieldName); }
            return field;
        }

        public bool TryGetField(VertexFieldSemantics semantics, [MaybeNullWhen(false)] out VertexFieldData field)
        {
            foreach(var f in _fields) {
                if(f.Semantics == semantics) {
                    field = f;
                    return true;
                }
            }
            field = null;
            return false;
        }

        public VertexFieldData GetField(VertexFieldSemantics semantics)
        {
            if(TryGetField(semantics, out var field) == false) { ThrowFieldSemanticsNotFound(semantics); }
            return field;
        }

        public bool HasField(string fieldName) => TryGetField(fieldName, out _);
        public bool HasField(VertexFieldSemantics semantics) => TryGetField(semantics, out _);

        public bool TryGetFieldAccessor<TField>(string fieldName, out VertexFieldAccessor<TField> accessor) where TField : unmanaged
        {
            if(TryGetField(fieldName, out var field)) {
                accessor = field.GetAccessor<TField>();
                return true;
            }
            accessor = default;
            return false;
        }

        public bool TryGetFieldAccessor<TField>(VertexFieldSemantics semantics, out VertexFieldAccessor<TField> accessor) where TField : unmanaged
        {
            if(TryGetField(semantics, out var field)) {
                accessor = field.GetAccessor<TField>();
                return true;
            }
            accessor = default;
            return false;
        }

        public VertexFieldAccessor<TField> GetFieldAccessor<TField>(string fieldName) where TField : unmanaged
        {
            if(TryGetFieldAccessor<TField>(fieldName, out var accessor) == false) {
                ThrowFieldNotFound(fieldName);
            }
            return accessor;
        }

        public VertexFieldAccessor<TField> GetFieldAccessor<TField>(VertexFieldSemantics semantics) where TField : unmanaged
        {
            if(TryGetFieldAccessor<TField>(semantics, out var accessor) == false) {
                ThrowFieldSemanticsNotFound(semantics);
            }
            return accessor;
        }

        [DoesNotReturn]
        private static void ThrowFieldSemanticsNotFound(VertexFieldSemantics semantics) => throw new InvalidOperationException($"The field semantics is not found. (semantics: {semantics})");

        [DoesNotReturn]
        private static void ThrowFieldNotFound(string fieldName) => throw new InvalidOperationException($"The field is not found. (Field Name: {fieldName})");
    }

    public record VertexFieldData(string Name, Type Type, VertexFieldSemantics Semantics, int ByteOffset, VertexFieldMarshalType MarshalType, int MarshalCount)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetRef<TVertex, T>(ref TVertex vertex)
        {
            return ref Unsafe.As<TVertex, T>(ref Unsafe.AddByteOffset(ref vertex, (nuint)ByteOffset));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref readonly T ReadRef<TVertex, T>(in TVertex vertex)
        {
            return ref Unsafe.As<TVertex, T>(ref Unsafe.AddByteOffset(ref Unsafe.AsRef(in vertex), (nuint)ByteOffset));
        }

        public VertexFieldAccessor<TField> GetAccessor<TField>() where TField : unmanaged
        {
            return new VertexFieldAccessor<TField>((nuint)ByteOffset);
        }
    }

    public readonly struct VertexFieldAccessor<TField> where TField : unmanaged
    {
        private readonly nuint _byteOffset;
        public nuint ByteOffset => _byteOffset;

        public VertexFieldAccessor(nuint byteOffset)
        {
            _byteOffset = byteOffset;
        }

        public ref TField Field<TVertex>(in TVertex vertex) where TVertex : unmanaged
        {
            return ref Unsafe.As<TVertex, TField>(ref Unsafe.AddByteOffset(ref Unsafe.AsRef(in vertex), _byteOffset));
        }
    }
}
