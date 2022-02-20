#nullable enable
using Elffy.Features;
using System;
using System.Runtime.CompilerServices;

namespace Elffy.Graphics.OpenGL
{
    public sealed class ShaderStorageBuffer : IDisposable
    {
        private Ssbo _ssbo;

        public Ssbo Ssbo => _ssbo;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ShaderStorageBuffer(Ssbo ssbo)
        {
            _ssbo = ssbo;
        }

        ~ShaderStorageBuffer() => Dispose(false);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ShaderStorageBuffer Create()
        {
            var screen = Engine.GetValidCurrentContext();
            var instance = new ShaderStorageBuffer(Ssbo.Create());
            ContextAssociatedMemorySafety.Register(instance, screen);
            return instance;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Bind() => Ssbo.Bind(_ssbo);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Unbind() => Ssbo.Unbind();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BindBase(int index) => Ssbo.BindBase(_ssbo, index);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BufferData<T>(ReadOnlySpan<T> data, BufferHint hint) where T : unmanaged
        {
            Ssbo.BufferData(data, hint);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void BufferDataUndefined<T>(int elementCount, BufferHint hint) where T : unmanaged
        {
            Ssbo.BufferData(elementCount * sizeof(T), null, hint);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void BufferDataUndefinedByte(int byteSize, BufferHint hint) => Ssbo.BufferData(byteSize, null, hint);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Dispose(bool disposing)
        {
            if(disposing) {
                Ssbo.Delete(ref _ssbo);
            }
            else {
                ContextAssociatedMemorySafety.OnFinalized(this);
            }
        }
    }
}
