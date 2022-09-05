#nullable enable
using Cysharp.Threading.Tasks;
using Elffy.Serialization.Gltf.Internal;
using System;

namespace Elffy.Serialization.Gltf;

internal class GlbModelPart : Renderable
{
}

internal sealed class GlbModelPart<TVertex> : GlbModelPart where TVertex : unmanaged
{
    private LargeBufferWriter<TVertex>? _verticesTemporal;
    private LargeBufferWriter<uint>? _indicesTemporal;
    private bool _meshApplied;

    internal GlbModelPart()
    {
    }

    internal ILargeBufferWriter<TVertex> GetVerticesWriter()
    {
        if(_meshApplied) {
            throw new InvalidOperationException();
        }
        _verticesTemporal ??= new LargeBufferWriter<TVertex>();
        return _verticesTemporal;
    }

    internal ILargeBufferWriter<uint> GetIndicesWriter()
    {
        if(_meshApplied) {
            throw new InvalidOperationException();
        }
        _indicesTemporal ??= new LargeBufferWriter<uint>();
        return _indicesTemporal;
    }

    internal async UniTask GetApplyMeshTask(IHostScreen screen)
    {
        await screen.Timings.Update.NextOrNow();
        ApplyMesh();
    }

    private unsafe void ApplyMesh()
    {
        if(_meshApplied) {
            throw new InvalidOperationException();
        }
        if(_verticesTemporal != null && _verticesTemporal.WrittenLength > 0) {
            nuint vLen = _verticesTemporal.WrittenLength;
            _indicesTemporal ??= new LargeBufferWriter<uint>();
            var indicesLen = _indicesTemporal.WrittenLength;
            if(indicesLen == 0) {
                if(vLen > uint.MaxValue) {
                    throw new NotSupportedException();
                }
                WriteSequentialNumbers(_indicesTemporal, (uint)vLen);
            }

            uint* indices = _indicesTemporal.GetWrittenBufffer(out var iLen);
            if(iLen > uint.MaxValue) {
                throw new NotSupportedException();
            }
            LoadMesh(_verticesTemporal.GetWrittenBufffer(out _), vLen, (int*)indices, (uint)iLen);
        }
        _meshApplied = true;
        _verticesTemporal?.Dispose();
        _indicesTemporal?.Dispose();
        _verticesTemporal = null;
        _indicesTemporal = null;
    }

    private unsafe static void WriteSequentialNumbers(ILargeBufferWriter<uint> output, uint length)
    {
        uint* ptr = output.GetBufferToWrite(length, false);
        for(uint i = 0; i < length; i++) {
            ptr[i] = i;
        }
        output.Advance(length);
    }
}
