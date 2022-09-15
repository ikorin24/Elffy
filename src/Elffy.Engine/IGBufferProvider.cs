#nullable enable
using Elffy.Shading.Deferred;
using System.Diagnostics.CodeAnalysis;

namespace Elffy
{
    internal interface IGBufferProvider
    {
        bool TryGetScreen([MaybeNullWhen(false)] out IHostScreen screen);

        GBufferData GetGBufferData();

        public IHostScreen GetValidScreen()
        {
            if(TryGetScreen(out var screen) == false) {
                ThrowHelper.ThrowInvalidNullScreen();
            }
            return screen;
        }
    }
}
