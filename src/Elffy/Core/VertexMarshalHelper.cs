#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Elffy.Diagnostics;

namespace Elffy.Core
{
    public static class VertexMarshalHelper<TVertex> where TVertex : unmanaged
    {
        private static VertexLayoutDelegate? _layouter;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static (int offset, VertexFieldMarshalType type, int elementCount) GetLayout(string fieldName)
        {
            var layouter = _layouter;
            if(layouter is null) {
                ThrowLayoutNotRegistered();
            }
            return layouter.Invoke(fieldName);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Register(VertexLayoutDelegate layout)
        {
            if(DevEnv.IsEnabled) {
                CheckVertexLikeType();
            }

            if(layout is null) {
                ThrowNullArg();
                [DoesNotReturn] static void ThrowNullArg() => throw new ArgumentNullException(nameof(layout));
            }
            if(_layouter is not null) {
                ThrowLayoutAlreadyRegistered();
            }
            _layouter = layout;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]  // no inlining
        private static void CheckVertexLikeType()
        {
            if(Attribute.GetCustomAttribute(typeof(TVertex), typeof(VertexLikeAttribute)) is null) {
                throw new ArgumentException($"Invalid type of vertex, which has no {nameof(VertexLikeAttribute)}");
            }
        }

        [DoesNotReturn]
        private static void ThrowLayoutNotRegistered() => throw new InvalidOperationException("Layouter is not registered");

        [DoesNotReturn]
        private static void ThrowLayoutAlreadyRegistered() => throw new InvalidOperationException("Layouter is already registered");
    }

    public delegate (int offset, VertexFieldMarshalType type, int elementCount) VertexLayoutDelegate(string fieldName);
}
