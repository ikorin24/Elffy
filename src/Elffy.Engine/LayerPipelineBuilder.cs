#nullable enable
using System;
using System.ComponentModel;
using Cysharp.Threading.Tasks;

namespace Elffy
{
    public record struct LayerPipelineBuilder
    {
        private readonly IHostScreen _screen;

        [Obsolete("Don't use default constructor.", true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public LayerPipelineBuilder() => throw new NotSupportedException("Don't use default constructor.");

        internal LayerPipelineBuilder(IHostScreen screen)
        {
            ArgumentNullException.ThrowIfNull(screen);
            _screen = screen;
        }

        public UniTask<L1> Build<L1>(Func<L1> factory1) where L1 : Layer
        {
            var screen = _screen;
            return factory1().Activate(screen);
        }

        public UniTask<(L1, L2)> Build<L1, L2>(
            Func<L1> factory1,
            Func<L2> factory2)
            where L1 : Layer
            where L2 : Layer
        {
            var screen = _screen;
            return UniTask.WhenAll(
                factory1().Activate(screen),
                factory2().Activate(screen)
                );
        }

        public UniTask<(L1, L2, L3)> Build<L1, L2, L3>(
            Func<L1> factory1,
            Func<L2> factory2,
            Func<L3> factory3
            )
            where L1 : Layer
            where L2 : Layer
            where L3 : Layer
        {
            var screen = _screen;
            return UniTask.WhenAll(
                factory1().Activate(screen),
                factory2().Activate(screen),
                factory3().Activate(screen)
                );
        }

        public UniTask<(L1, L2, L3, L4)> Build<L1, L2, L3, L4>(
            Func<L1> factory1,
            Func<L2> factory2,
            Func<L3> factory3,
            Func<L4> factory4
            )
            where L1 : Layer
            where L2 : Layer
            where L3 : Layer
            where L4 : Layer
        {
            var screen = _screen;
            return UniTask.WhenAll(
                factory1().Activate(screen),
                factory2().Activate(screen),
                factory3().Activate(screen),
                factory4().Activate(screen)
                );
        }

        public UniTask<(L1, L2, L3, L4, L5)> Build<L1, L2, L3, L4, L5>(
            Func<L1> factory1,
            Func<L2> factory2,
            Func<L3> factory3,
            Func<L4> factory4,
            Func<L5> factory5
            )
            where L1 : Layer
            where L2 : Layer
            where L3 : Layer
            where L4 : Layer
            where L5 : Layer
        {
            var screen = _screen;
            return UniTask.WhenAll(
                factory1().Activate(screen),
                factory2().Activate(screen),
                factory3().Activate(screen),
                factory4().Activate(screen),
                factory5().Activate(screen)
                );
        }

        public UniTask<(L1, L2, L3, L4, L5, L6)> Build<L1, L2, L3, L4, L5, L6>(
            Func<L1> factory1,
            Func<L2> factory2,
            Func<L3> factory3,
            Func<L4> factory4,
            Func<L5> factory5,
            Func<L6> factory6
            )
            where L1 : Layer
            where L2 : Layer
            where L3 : Layer
            where L4 : Layer
            where L5 : Layer
            where L6 : Layer
        {
            var screen = _screen;
            return UniTask.WhenAll(
                factory1().Activate(screen),
                factory2().Activate(screen),
                factory3().Activate(screen),
                factory4().Activate(screen),
                factory5().Activate(screen),
                factory6().Activate(screen)
                );
        }

        public UniTask<(L1, L2, L3, L4, L5, L6, L7)> Build<L1, L2, L3, L4, L5, L6, L7>(
            Func<L1> factory1,
            Func<L2> factory2,
            Func<L3> factory3,
            Func<L4> factory4,
            Func<L5> factory5,
            Func<L6> factory6,
            Func<L7> factory7
            )
            where L1 : Layer
            where L2 : Layer
            where L3 : Layer
            where L4 : Layer
            where L5 : Layer
            where L6 : Layer
            where L7 : Layer
        {
            var screen = _screen;
            return UniTask.WhenAll(
                factory1().Activate(screen),
                factory2().Activate(screen),
                factory3().Activate(screen),
                factory4().Activate(screen),
                factory5().Activate(screen),
                factory6().Activate(screen),
                factory7().Activate(screen)
                );
        }

        public UniTask<(L1, L2, L3, L4, L5, L6, L7, L8)> Build<L1, L2, L3, L4, L5, L6, L7, L8>(
            Func<L1> factory1,
            Func<L2> factory2,
            Func<L3> factory3,
            Func<L4> factory4,
            Func<L5> factory5,
            Func<L6> factory6,
            Func<L7> factory7,
            Func<L8> factory8
            )
            where L1 : Layer
            where L2 : Layer
            where L3 : Layer
            where L4 : Layer
            where L5 : Layer
            where L6 : Layer
            where L7 : Layer
            where L8 : Layer
        {
            var screen = _screen;
            return UniTask.WhenAll(
                factory1().Activate(screen),
                factory2().Activate(screen),
                factory3().Activate(screen),
                factory4().Activate(screen),
                factory5().Activate(screen),
                factory6().Activate(screen),
                factory7().Activate(screen),
                factory8().Activate(screen)
                );
        }

        public UniTask<Layer[]> Build(params Func<Layer>[] factories)
        {
            return Build(factories.AsSpan());
        }

        public UniTask<Layer[]> Build(ReadOnlySpan<Func<Layer>> factories)
        {
            var screen = _screen;
            var layers = new UniTask<Layer>[factories.Length];
            for(int i = 0; i < factories.Length; i++) {
                layers[i] = factories[i].Invoke().Activate(screen);
            }
            return UniTask.WhenAll(layers);
        }
    }
}
