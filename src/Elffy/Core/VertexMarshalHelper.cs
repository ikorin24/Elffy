#nullable enable
using System;
using System.Runtime.CompilerServices;
using Elffy.Diagnostics;

namespace Elffy.Core
{
    public static class VertexMarshalHelper<T> where T : unmanaged
    {
        private static VertexLayoutDelegate? _layouter;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static (int offset, VertexFieldMarshalType type, int elementCount) GetLayout(string fieldName)
        {
            var layouter = _layouter;
            if(layouter is null) {
                ThrowLayoutNotRegistered();
            }
            return layouter!.Invoke(fieldName);

            static void ThrowLayoutNotRegistered() => throw new InvalidOperationException("Layouter is not registered");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Register(VertexLayoutDelegate layout)
        {
            if(DevEnv.IsEnabled) {
                if(Attribute.GetCustomAttribute(typeof(T), typeof(VertexLikeAttribute)) is null) {
                    throw new ArgumentException($"Invalid type of vertex, which has no {nameof(VertexLikeAttribute)}");
                }
            }

            if(_layouter is null == false) { throw new InvalidOperationException("already registerd"); }
            _layouter = layout ?? throw new ArgumentNullException(nameof(layout));
        }
    }

    public delegate (int offset, VertexFieldMarshalType type, int elementCount) VertexLayoutDelegate(string fieldName);
}
