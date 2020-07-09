#nullable enable
using System;
using System.Runtime.CompilerServices;
using OpenToolkit.Graphics.OpenGL;

namespace Elffy.Core
{
    public static class VertexMarshalHelper<T> where T : unmanaged
    {
        private static VertexLayoutDelegate? _layouter;

        public static VertexLayoutDelegate Layout
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _layouter ?? throw new InvalidOperationException("Layouter is not registered");
        }

        public static void Register(VertexLayoutDelegate layout)
        {
            if(_layouter is null == false) { throw new InvalidOperationException("already registerd"); }
            _layouter = layout ?? throw new ArgumentNullException(nameof(layout));
        }
    }

    public delegate (int offset, VertexFieldElementType type, int elementCount) VertexLayoutDelegate(string fieldName);

    public enum VertexFieldElementType
    {
        Byte = VertexAttribPointerType.UnsignedByte,
        Int16 = VertexAttribPointerType.Short,
        Int32 = VertexAttribPointerType.Int,
        Uint32 = VertexAttribPointerType.UnsignedInt,
        HalfFloat = VertexAttribPointerType.HalfFloat,
        Float = VertexAttribPointerType.Float,
        Double = VertexAttribPointerType.Double,
    }
}
