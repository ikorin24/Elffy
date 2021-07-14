#nullable enable
using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Elffy.Diagnostics;

namespace Elffy.Core
{
    public static class VertexMarshalHelper
    {
        private static readonly object _lockObj = new object();
        private static readonly Hashtable _dic = new Hashtable();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Register<TVertex>(VertexLayoutDelegate layout, VertexSpecialFieldMapDelegate? specialFieldMap = null) where TVertex : unmanaged
        {
            var type = typeof(TVertex);
            if(DevEnv.IsEnabled) {
                CheckVertexLikeType(type);
            }

            if(layout is null) {
                ThrowNullArg();
                [DoesNotReturn] static void ThrowNullArg() => throw new ArgumentNullException(nameof(layout));
            }

            var vertexSize = Unsafe.SizeOf<TVertex>();

            specialFieldMap ??= _ => "";

            lock(_lockObj) {
                var data = new VertexTypeData(vertexSize, layout, specialFieldMap);
                _dic.Add(type, data);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VertexTypeData GetVertexTypeData(Type type)
        {
            return SafeCast.As<VertexTypeData>(_dic[type]);
        }

        public static (int offset, VertexFieldMarshalType type, int elementCount, int vertexSize) GetLayout(Type type, VertexSpecialField specialField)
        {
            var data = GetVertexTypeData(type);
            var specialFieldMap = data.SpecialFieldMap;
            if(specialFieldMap is null) {
                ThrowInvalidOp();
                [DoesNotReturn] static void ThrowInvalidOp() => throw new InvalidOperationException("special fields are not registerd.");
            }
            var fieldName = specialFieldMap.Invoke(specialField);
            var (offset, fieldType, elementCount) = data.Layouter.Invoke(fieldName);
            return (offset, fieldType, elementCount, data.VertexSize);
        }

        public static (int offset, VertexFieldMarshalType type, int elementCount, int vertexSize) GetLayout(Type type, string fieldName)
        {
            var data = GetVertexTypeData(type);
            var (offset, fieldType, elementCount) = data.Layouter.Invoke(fieldName);
            return (offset, fieldType, elementCount, data.VertexSize);
        }

        public static bool HasSpecialField(Type vertexType, VertexSpecialField specialField)
        {
            RuntimeHelpers.RunClassConstructor(vertexType.TypeHandle);
            var data = GetVertexTypeData(vertexType);
            try {
                var fieldName = data.SpecialFieldMap.Invoke(specialField);
                return !string.IsNullOrEmpty(fieldName);
            }
            catch {
                return false;
            }
        }

        private static void CheckVertexLikeType(Type type)
        {
            if(type.IsValueType == false) {
                throw new ArgumentException($"Vertex type must be struct");
            }
            if(Attribute.GetCustomAttribute(type, typeof(VertexLikeAttribute)) is null) {
                throw new ArgumentException($"Invalid type of vertex, which has no {nameof(VertexLikeAttribute)}");
            }
        }
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
