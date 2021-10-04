#nullable enable
using System;
using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Elffy
{
    public static class VertexMarshalHelper
    {
        private static readonly object _lockObj = new object();
        private static readonly Hashtable _dic = new Hashtable();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VertexMarshalRegisterResult Register<TVertex>(VertexLayoutDelegate layout, VertexSpecialFieldMapDelegate? specialFieldMap = null) where TVertex : unmanaged
        {
            var type = typeof(TVertex);
            var ex = CheckVertexLikeType(type);
            if(ex is not null) {
                return VertexMarshalRegisterResult.Error(ex);
            }
            if(layout is null) { ThrowNullArg(nameof(layout)); }
            var vertexSize = Unsafe.SizeOf<TVertex>();
            specialFieldMap ??= _ => "";
            lock(_lockObj) {
                var data = new VertexTypeData(vertexSize, layout, specialFieldMap);
                _dic.Add(type, data);
            }
            return VertexMarshalRegisterResult.Success;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetVertexTypeData(Type type, [MaybeNullWhen(false)] out VertexTypeData typeData)
        {
            var data = _dic[type];
            Debug.Assert(data is null or VertexTypeData);
            typeData = Unsafe.As<VertexTypeData>(data);
            return typeData != null;
        }

        public static (int offset, VertexFieldMarshalType type, int elementCount, int vertexSize) GetLayout(Type vertexType, VertexSpecialField specialField)
        {
            if(TryGetVertexTypeData(vertexType, out var data) == false) {
                ThrowTypeNotRegistered(vertexType);
            }
            var specialFieldMap = data.SpecialFieldMap;
            if(specialFieldMap is null) {
                ThrowSpecialFieldsNotRegistered();
            }
            var fieldName = specialFieldMap.Invoke(specialField);
            var (offset, fieldType, elementCount) = data.Layouter.Invoke(fieldName);
            return (offset, fieldType, elementCount, data.VertexSize);
        }

        public static (int offset, VertexFieldMarshalType type, int elementCount, int vertexSize) GetLayout(Type vertexType, string fieldName)
        {
            if(TryGetVertexTypeData(vertexType, out var data) == false) {
                ThrowTypeNotRegistered(vertexType);
            }
            var (offset, fieldType, elementCount) = data.Layouter.Invoke(fieldName);
            return (offset, fieldType, elementCount, data.VertexSize);
        }

        public static bool HasSpecialField(Type vertexType, VertexSpecialField specialField)
        {
            RuntimeHelpers.RunClassConstructor(vertexType.TypeHandle);
            if(TryGetVertexTypeData(vertexType, out var data) == false) {
                ThrowTypeNotRegistered(vertexType);
            }
            try {
                var fieldName = data.SpecialFieldMap.Invoke(specialField);
                return !string.IsNullOrEmpty(fieldName);
            }
            catch {
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Exception? CheckVertexLikeType(Type vertexType)
        {
            if(vertexType.IsValueType == false) {
                return new ArgumentException($"Vertex type must be struct. (Type = {vertexType.FullName})");
            }
            if(Attribute.GetCustomAttribute(vertexType, typeof(VertexLikeAttribute)) is null) {
                return new ArgumentException($"Invalid type of vertex, which has no {nameof(VertexLikeAttribute)} (Type = {vertexType.FullName})");
            }
            return null;
        }

        [DoesNotReturn]
        private static void ThrowNullArg(string message) => throw new ArgumentNullException(message);

        [DoesNotReturn]
        private static void ThrowSpecialFieldsNotRegistered() => throw new InvalidOperationException("Special fields are not registerd.");

        [DoesNotReturn]
        private static void ThrowTypeNotRegistered(Type type) => throw new ArgumentException($"The type is not registered. (Type = {type.FullName})");
    }

    public readonly struct VertexMarshalRegisterResult : IEquatable<VertexMarshalRegisterResult>
    {
        private readonly Exception? _exception;
        public bool IsSuccess => _exception is null;

        internal static VertexMarshalRegisterResult Success => new VertexMarshalRegisterResult(null);

        private VertexMarshalRegisterResult(Exception? exception) => _exception = exception;

        internal static VertexMarshalRegisterResult Error(Exception exception) => new VertexMarshalRegisterResult(exception);

        public void ThrowIfError()
        {
            if(_exception is not null) { throw _exception; }
        }

        public override bool Equals(object? obj) => obj is VertexMarshalRegisterResult result && Equals(result);

        public bool Equals(VertexMarshalRegisterResult other) => _exception == other._exception;

        public override int GetHashCode() => _exception?.GetHashCode() ?? 0;
    }

    public sealed class VertexTypeData
    {
        public VertexLayoutDelegate Layouter { get; }
        public VertexSpecialFieldMapDelegate SpecialFieldMap { get; }
        public int VertexSize { get; }
        public VertexTypeData(int vertexSize, VertexLayoutDelegate layouter, VertexSpecialFieldMapDelegate specialFieldMap)
        {
            VertexSize = vertexSize;
            Layouter = layouter;
            SpecialFieldMap = specialFieldMap;
        }
    }

    public delegate (int offset, VertexFieldMarshalType type, int elementCount) VertexLayoutDelegate(string fieldName);

    public delegate string VertexSpecialFieldMapDelegate(VertexSpecialField specialField);
}
