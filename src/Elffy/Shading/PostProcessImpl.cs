#nullable enable
using System;
using System.Runtime.CompilerServices;

namespace Elffy.Shading
{
    /// <summary>Implementation helper struct for <see cref="Shading.PostProcess"/></summary>
    internal struct PostProcessImpl : IDisposable
    {
        private bool _postProcessChanged;
        private PostProcessCompiled? _ppCompiled;
        private PostProcess? _postProcess;

        public PostProcess? PostProcess
        {
            get => _postProcess;
            set
            {
                _postProcess = value;
                _postProcessChanged = true;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ApplyChange()
        {
            if(_postProcessChanged) {
                ApplyChangePrivate();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FrameBufferScope GetScopeAsRoot(in Vector2i screenSize)
        {
            return FrameBufferScope.RootScope(_ppCompiled, screenSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FrameBufferScope GetScope(in FrameBufferScope parentScope)
        {
            return parentScope.NewScope(_ppCompiled);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            _ppCompiled?.Dispose();
            _ppCompiled = null;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]  // uncommon path
        private void ApplyChangePrivate()
        {
            _ppCompiled?.Dispose();
            _ppCompiled = _postProcess?.Compile();
            _postProcessChanged = false;
        }
    }
}
