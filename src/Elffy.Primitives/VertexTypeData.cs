#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;

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

        public bool TryGetField(VertexSpecialField specialField, [MaybeNullWhen(false)] out VertexFieldData field)
        {
            foreach(var f in _fields) {
                if(f.SpecialField == specialField) {
                    field = f;
                    return true;
                }
            }
            field = null;
            return false;
        }

        public VertexFieldData GetField(VertexSpecialField specialField)
        {
            if(TryGetField(specialField, out var field) == false) { ThrowSpecialFieldNotFound(specialField); }
            return field;
        }

        [DoesNotReturn]
        private static void ThrowSpecialFieldNotFound(VertexSpecialField specialField) => throw new InvalidOperationException($"The special field is not found. (Special Field: {specialField})");

        [DoesNotReturn]
        private static void ThrowFieldNotFound(string fieldName) => throw new InvalidOperationException($"The field is not found. (Field Name: {fieldName})");
    }

    public record VertexFieldData(string Name, Type Type, VertexSpecialField SpecialField, int ByteOffset, VertexFieldMarshalType MarshalType, int MarshalCount);
}
