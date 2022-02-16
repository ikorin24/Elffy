#nullable enable
using Elffy.Features;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Runtime.CompilerServices;

namespace Elffy.Graphics.OpenGL
{
    public sealed class ShaderStorageBuffer : IDisposable
    {
        private Ssbo _ssbo;

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
        public void BufferData<T>(ReadOnlySpan<T> data, BufferUsageHint usage) where T : unmanaged => Ssbo.BufferData(data, usage);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void BufferDataUndefined(int byteSize, BufferUsageHint usage) => Ssbo.BufferData(byteSize, null, usage);

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
