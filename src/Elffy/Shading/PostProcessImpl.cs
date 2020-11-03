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
        public PostProcessCompiled? GetCompiled()
        {
            if(_postProcessChanged) {
                ApplyChangePrivate();
            }
            return _ppCompiled;
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
