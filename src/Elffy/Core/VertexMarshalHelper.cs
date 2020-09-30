#nullable enable
using System;
using System.Runtime.CompilerServices;
using Elffy.AssemblyServices;
using Elffy.Diagnostics;
using OpenToolkit.Graphics.OpenGL4;

namespace Elffy.Core
{
    public static class VertexMarshalHelper<T> where T : unmanaged
    {
        private static VertexLayoutDelegate? _layouter;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static (int offset, VertexFieldElementType type, int elementCount) GetLayout(string fieldName)
        {
            var layouter = _layouter;
            if(layouter is null) {
                ThrowLayoutNotRegistered();
            }
            return layouter!.Invoke(fieldName);

            static void ThrowLayoutNotRegistered() => throw new InvalidOperationException("Layouter is not registered");
        }

        public static void Register(VertexLayoutDelegate layout)
        {
            if(AssemblyState.IsDevelop && DevelopingDiagnostics.IsEnabled) {
                if(Attribute.GetCustomAttribute(typeof(T), typeof(VertexLikeAttribute)) is null) {
                    throw new ArgumentException($"Invalid type of vertex, which has no {nameof(VertexLikeAttribute)}");
                }
            }

            if(_layouter is null == false) { throw new InvalidOperationException("already registerd"); }
            _layouter = layout ?? throw new ArgumentNullException(nameof(layout));
        }
    }

    public delegate (int offset, VertexFieldElementType type, int elementCount) VertexLayoutDelegate(string fieldName);

    public enum VertexFieldElementType
    {
        // floating point types

        Float = VertexAttribPointerType.Float,
        HalfFloat = VertexAttribPointerType.HalfFloat,

        // integer types

        Uint32 = VertexAttribIntegerType.UnsignedInt,
        Int32 = VertexAttribIntegerType.Int,
        Byte = VertexAttribIntegerType.UnsignedByte,
        Int16 = VertexAttribIntegerType.Short,
        Uint16 = VertexAttribIntegerType.UnsignedShort,
    }
}
