#nullable enable

using Elffy;

namespace Elffy.Serialization.Gltf.Internal;

internal unsafe interface ILargeBufferWriter<T> where T : unmanaged
{
    void Advance(nuint count);
    T* GetWrittenBufffer(out nuint count);
    T* GetBufferToWrite(nuint count, bool zeroCleared);
}
