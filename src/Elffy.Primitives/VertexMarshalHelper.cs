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
        private static class TypeDicStatic<TVertex> where TVertex : unmanaged
        {
            public static VertexTypeData? Value { get; set; }
        }

        private static readonly object _lockObj = new object();
        private static readonly Hashtable _typeDic = new Hashtable();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VertexMarshalRegisterResult Register<TVertex>(VertexFieldData[] fields) where TVertex : unmanaged
        {
            var type = typeof(TVertex);
            var ex = CheckVertexLikeType(type);
            if(ex is not null) {
                return VertexMarshalRegisterResult.Error(ex);
            }
            if(fields is null) { ThrowNullArg(nameof(fields)); }
            var vertexSize = Unsafe.SizeOf<TVertex>();
            var typeData = new VertexTypeData(fields, vertexSize);
            lock(_lockObj) {
                _typeDic.Add(type, typeData);
                TypeDicStatic<TVertex>.Value = typeData;
            }
            return VertexMarshalRegisterResult.Success;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetVertexTypeData<TVertex>([MaybeNullWhen(false)] out VertexTypeData typeData) where TVertex : unmanaged
        {
            var value = TypeDicStatic<TVertex>.Value;
            typeData = value;
            return value is not null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetVertexTypeData(Type vertexType, [MaybeNullWhen(false)] out VertexTypeData typeData)
        {
            var data = _typeDic[vertexType];
            Debug.Assert(data is null or VertexTypeData);
            typeData = Unsafe.As<VertexTypeData>(data);
            return typeData != null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VertexTypeData GetVertexTypeData<TVertex>() where TVertex : unmanaged
        {
            if(TryGetVertexTypeData<TVertex>(out var typeData) == false) { ThrowTypeNotRegistered(typeof(TVertex)); }
            return typeData;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VertexTypeData GetVertexTypeData(Type vertexType)
        {
            if(TryGetVertexTypeData(vertexType, out var typeData) == false) { ThrowTypeNotRegistered(vertexType); }
            return typeData;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Exception? CheckVertexLikeType(Type vertexType)
        {
            if(vertexType.IsValueType == false) {
                return new ArgumentException($"Vertex type must be struct. (Type = {vertexType.FullName})");
            }
            if(VertexAttribute.IsVertexType(vertexType) == false) {
                return new ArgumentException($"Invalid type of vertex, which has no {nameof(VertexAttribute)} (Type = {vertexType.FullName})");
            }
            return null;
        }

        [DoesNotReturn]
        private static void ThrowNullArg(string message) => throw new ArgumentNullException(message);

        [DoesNotReturn]
        private static void ThrowTypeNotRegistered(Type vertexType) => throw new ArgumentException($"The type is not registered. (Type = {vertexType.FullName})");
    }

    public readonly struct VertexMarshalRegisterResult : IEquatable<VertexMarshalRegisterResult>
    {
        private readonly Exception? _exception;
        public bool IsSuccess => _exception is null;

        internal static VertexMarshalRegisterResult Success => new VertexMarshalRegisterResult(null);

        private VertexMarshalRegisterResult(Exception? exception) => _exception = exception;

        internal static VertexMarshalRegisterResult Error(Exception exception) => new VertexMarshalRegisterResult(exception);

        [DebuggerHidden]
        public void ThrowIfError()
        {
            if(_exception is not null) { throw _exception; }
        }

        public override bool Equals(object? obj) => obj is VertexMarshalRegisterResult result && Equals(result);

        public bool Equals(VertexMarshalRegisterResult other) => _exception == other._exception;

        public override int GetHashCode() => _exception?.GetHashCode() ?? 0;
    }
}
