#nullable enable
using Cysharp.Threading.Tasks;
using Elffy.Effective;
using Elffy.Effective.Unsafes;
using Elffy.Serialization.Gltf.Internal;
using Elffy.Shapes;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Threading;

namespace Elffy.Serialization.Gltf;

public static class GlbModelBuilder
{
    private sealed record StateObject(ResourceFile File, CancellationToken CancellationToken);

    private static readonly Model3DBuilderDelegate<StateObject> _build = Build;

    public static Model3D CreateLazyLoadingGlb(ResourceFile file, CancellationToken cancellationToken = default)
    {
        ResourceFile.ThrowArgumentExceptionIfNone(file);
        var obj = new StateObject(file, cancellationToken);
        return Model3D.Create(obj, _build);
    }

    private static async UniTask Build(StateObject state, Model3D model, Model3DLoadMeshDelegate load)
    {
        var (file, ct) = state;
        var screen = model.GetValidScreen();
        ct.ThrowIfCancellationRequested();

        await UniTask.SwitchToThreadPool();
        ct.ThrowIfCancellationRequested();

        using var glb = ParseGlb(file, ct);

        //using var verticesBuffer = new UnsafeBufferWriter<Vertex>();
        //using var indicesBuffer = new UnsafeBufferWriter<int>();

        using var verticesBuffer = new LargeBufferWriter<Vertex>();
        using var indicesBuffer = new LargeBufferWriter<uint>();

        BuildRoot(verticesBuffer, indicesBuffer, glb, model, load);

        await screen.Timings.Update.Next(ct);

        // TODO: not implemented yet
        //load.Invoke(verticesBuffer.WrittenSpan, indicesBuffer.WrittenSpan);
        load.Invoke(ReadOnlySpan<Vertex>.Empty, ReadOnlySpan<int>.Empty);
    }

    private static unsafe GlbObject ParseGlb(ResourceFile file, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if(file.TryGetHandle(out var handle)) {
            var len = (nuint)file.FileSize;
            void* ptr = NativeMemory.Alloc(len);
            try {
                handle.Read(ptr, len, 0);
                return GltfParser.ParseGlb(ptr, len);
            }
            finally {
                NativeMemory.Free(ptr);
            }
        }
        else {
            nuint len = (nuint)file.FileSize;
            byte* ptr = (byte*)NativeMemory.Alloc(len);
            try {
                using var stream = file.GetStream();
                ulong pos = 0;
                while(true) {
                    ct.ThrowIfCancellationRequested();
                    int spanLen = (int)Math.Min(int.MaxValue, len - pos);
                    var span = new Span<byte>(ptr + pos, spanLen);
                    var readlen = stream.Read(span);
                    pos += (ulong)readlen;
                    if(readlen == 0) { break; }
                }
                return GltfParser.ParseGlb(ptr, len);
            }
            finally {
                NativeMemory.Free(ptr);
            }
        }
    }

    private unsafe static void BuildRoot(LargeBufferWriter<Vertex> verticesOutput, LargeBufferWriter<uint> indicesOutput, GlbObject glb, Model3D model, Model3DLoadMeshDelegate load)
    {
        var gltf = glb.Gltf;

        ValidateVersion(gltf);

        if(gltf.scene.TryGetValue(out var sceneNum)) {
            ref readonly var defaultScene = ref GetItemOrThrow(gltf.scenes, sceneNum);
            if(defaultScene.nodes != null) {
                foreach(var nodeNum in defaultScene.nodes) {
                    ref readonly var node = ref GetItemOrThrow(gltf.nodes, nodeNum);
                    if(node.mesh.TryGetValue(out var meshNum)) {
                        ref readonly var mesh = ref GetItemOrThrow(gltf.meshes, meshNum);
                        foreach(ref readonly var meshPrimitive in mesh.primitives.AsSpan()) {
                            switch(meshPrimitive.mode) {
                                case MeshPrimitiveMode.Points:
                                case MeshPrimitiveMode.Lines:
                                case MeshPrimitiveMode.LineLoop:
                                case MeshPrimitiveMode.LineStrip: {
                                    throw new NotImplementedException();
                                }
                                case MeshPrimitiveMode.Triangles: {
                                    ref readonly var attrs = ref meshPrimitive.attributes;
                                    if(meshPrimitive.indices.TryGetValue(out var indicesNum)) {
                                        ref readonly var indices = ref GetItemOrThrow(gltf.accessors, indicesNum);
                                        if(indices.type != AccessorType.Scalar) {
                                            ThrowInvalidGlb();
                                        }
                                        ReadBufferData(glb, indices, indicesOutput, static (output, ptr, byteLength, type) =>
                                        {
                                            switch(type) {
                                                case AccessorComponentType.UnsignedByte: {
                                                    uint* dest = output.GetBufferToWrite(byteLength);
                                                    ConvertUInt8ToUInt32((byte*)ptr, dest, byteLength);
                                                    output.Advance(byteLength);
                                                    break;
                                                }
                                                case AccessorComponentType.UnsignedShort: {
                                                    nuint l = byteLength / sizeof(ushort);
                                                    uint* dest = output.GetBufferToWrite(l);
                                                    ConvertUInt16ToUInt32((ushort*)ptr, dest, l);
                                                    output.Advance(l);
                                                    break;
                                                }
                                                case AccessorComponentType.UnsignedInt: {
                                                    nuint l = byteLength / sizeof(uint);
                                                    uint* dest = output.GetBufferToWrite(l);
                                                    System.Buffer.MemoryCopy(ptr, dest, byteLength, byteLength);
                                                    if(BitConverter.IsLittleEndian == false) {
                                                        ReverseEndian(dest, l);
                                                    }
                                                    output.Advance(l);
                                                    break;
                                                }
                                                default: {
                                                    ThrowInvalidGlb();
                                                    break;
                                                }

                                            }
                                        });
                                    }
                                    else {

                                        // TODO: other attributes
                                    }
                                    break;
                                }
                                case MeshPrimitiveMode.TriangleStrip:
                                case MeshPrimitiveMode.TriangleFan:
                                default: {
                                    throw new NotImplementedException();
                                }
                            }
                        }
                    }
                }
            }
        }

        // not implemented yet
    }

    private static void ValidateVersion(GltfObject gltf)
    {
        var version = gltf.asset.version.AsSpan();
        if(version.SequenceEqual(stackalloc byte[] { (byte)'2', (byte)'.', (byte)'0' }) == false) {
            throw new NotSupportedException("only supports gltf v2.0");
        }
    }

    private unsafe delegate void ReadBufferAction<T>(T state, void* ptr, nuint byteLength, AccessorComponentType type);

    private unsafe static void ReadBufferData<T>(GlbObject glb, in Accessor accessor, T state, ReadBufferAction<T> onRead)
    {
        // LargeBufferWriter<T> output
        var gltf = glb.Gltf;

        if(accessor.bufferView.TryGetValue(out var bufferViewNum) == false) {
            return;
        }

        ref readonly var bufferView = ref GetItemOrThrow(gltf.bufferViews, bufferViewNum);
        ref readonly var buffer = ref GetItemOrThrow(gltf.buffers, bufferView.buffer);
        if(buffer.uri == null) {
            if(bufferView.byteStride.TryGetValue(out var stride)) {
                throw new NotImplementedException();
            }
            else {
                nuint offset = bufferView.byteOffset;
                nuint len = bufferView.byteLength;
                var bin = glb.GetBinaryData(bufferView.buffer).Slice(offset, len);

                if(bin.ByteLength > int.MaxValue) {
                    throw new NotSupportedException();
                }
                onRead.Invoke(state, bin.Ptr, bin.ByteLength, accessor.componentType);
            }
        }
        else {
            throw new NotImplementedException();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ref T GetItemOrThrow<T>(T[]? array, uint index)
    {
        if(array == null) {
            ThrowInvalidGlb();
        }
        return ref array[index];
    }

    private unsafe static void ReverseEndian(uint* p, nuint count)
    {
        for(nuint i = 0; i < count; i++) {
            p[i] = ((p[i] & 0x0000_00FF) << 24) + ((p[i] & 0x0000_FF00) << 8) + ((p[i] & 0x00FF_0000) >> 8) + ((p[i] & 0xFF00_0000) >> 24);
        }
    }

    [DoesNotReturn]
    private static void ThrowInvalidGlb() => throw new FormatException("invalid glb");

    private static unsafe void ConvertUInt16ToUInt32(ushort* src, uint* dest, nuint elementCount)
    {
        if(Sse2.IsSupported && Avx2.IsSupported) {
            // extend each packed u16 to u32
            //
            // <u16, u16, u16, u16, u16, u16, u16, u16> (128 bits)
            //   |    |    |    |    |    |    |    |
            //   |    |    |    |    |    |    |    | 
            // <u32, u32, u32, u32, u32, u32, u32, u32> (256 bits)

            var (n, m) = Math.DivRem(elementCount, 8);

            const uint LoopUnrollFactor = 4;
            var (n1, n2) = Math.DivRem(n, LoopUnrollFactor);
            for(nuint i = 0; i < n1; i++) {
                var x = i * 8 * LoopUnrollFactor;
                Unsafe.As<uint, Vector256<int>>(ref dest[x]) = Avx2.ConvertToVector256Int32(Sse2.LoadVector128(&src[x]));
                Unsafe.As<uint, Vector256<int>>(ref dest[x + 8]) = Avx2.ConvertToVector256Int32(Sse2.LoadVector128(&src[x + 8]));
                Unsafe.As<uint, Vector256<int>>(ref dest[x + 16]) = Avx2.ConvertToVector256Int32(Sse2.LoadVector128(&src[x + 16]));
                Unsafe.As<uint, Vector256<int>>(ref dest[x + 24]) = Avx2.ConvertToVector256Int32(Sse2.LoadVector128(&src[x + 24]));
            }
            var offset = n1 * 8 * LoopUnrollFactor;
            for(nuint i = 0; i < n2; i++) {
                var x = offset + i * 8;
                Unsafe.As<uint, Vector256<int>>(ref dest[x]) = Avx2.ConvertToVector256Int32(Sse2.LoadVector128(&src[x]));
            }
            offset += n2 * 8;
            for(nuint i = 0; i < m; i++) {
                dest[offset + i] = (uint)src[offset + i];
            }
        }
        else {
            NonVectorFallback(src, dest, elementCount);
        }

        static void NonVectorFallback(ushort* src, uint* dest, nuint elementCount)
        {
            for(nuint i = 0; i < elementCount; i++) {
                dest[i] = (uint)src[i];
            }
        }
    }

    private static unsafe void ConvertUInt8ToUInt32(byte* src, uint* dest, nuint elementCount)
    {
        if(Sse2.IsSupported && Avx2.IsSupported) {
            // extend each packed u8 to u32
            // 
            // (uint8 * 16) is packed in 128 bits,
            // but 'Avx2.ConvertToVector256Int32' method converts only eight packed uint8 in lower 64 bits.

            // 128 bits
            // <u8, u8, u8, u8, u8, u8, u8, u8, u8, u8, u8, u8, u8, u8, u8, u8>
            //                                  ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            //                                              | lower 64 bits
            // 256 bits                                     |
            // <u32, u32, u32, u32, u32, u32, u32, u32>  <--'

            var (n, m) = Math.DivRem(elementCount, 8);

            const uint LoopUnrollFactor = 4;
            var (n1, n2) = Math.DivRem(n, LoopUnrollFactor);
            for(nuint i = 0; i < n1; i++) {
                var x = i * 8 * LoopUnrollFactor;
                Unsafe.As<uint, Vector256<int>>(ref dest[x]) = Avx2.ConvertToVector256Int32(Sse2.LoadVector128(&src[x]));
                Unsafe.As<uint, Vector256<int>>(ref dest[x + 8]) = Avx2.ConvertToVector256Int32(Sse2.LoadVector128(&src[x + 8]));
                Unsafe.As<uint, Vector256<int>>(ref dest[x + 16]) = Avx2.ConvertToVector256Int32(Sse2.LoadVector128(&src[x + 16]));
                Unsafe.As<uint, Vector256<int>>(ref dest[x + 24]) = Avx2.ConvertToVector256Int32(Sse2.LoadVector128(&src[x + 24]));
            }
            var offset = n1 * 8 * LoopUnrollFactor;
            for(nuint i = 0; i < n2; i++) {
                var x = offset + i * 8;
                Unsafe.As<uint, Vector256<int>>(ref dest[x]) = Avx2.ConvertToVector256Int32(Sse2.LoadVector128(&src[x]));
            }
            offset += n2 * 8;
            for(nuint i = 0; i < m; i++) {
                dest[offset + i] = (uint)src[offset + i];
            }
        }
        else {
            NonVectorFallback(src, dest, elementCount);
        }

        static void NonVectorFallback(byte* src, uint* dest, nuint elementCount)
        {
            for(nuint i = 0; i < elementCount; i++) {
                dest[i] = (uint)src[i];
            }
        }
    }
}

internal unsafe sealed class LargeBufferWriter<T> : IDisposable where T : unmanaged
{
    private NativeBuffer _buf;
    private nuint _count;
    private nuint Capacity
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _buf.ByteLength / (nuint)sizeof(T);
    }

    public void Advance(nuint count)
    {
        if(count > Capacity - _count) {
            ThrowArg();
            [DoesNotReturn] static void ThrowArg() => throw new ArgumentException($"{nameof(count)} is too large", nameof(count));
        }
        _count += count;
    }

    public LargeBufferWriter() : this(0)
    {
    }

    public LargeBufferWriter(nuint initialCapacity)
    {
        var byteCapacity = checked(initialCapacity * (nuint)sizeof(T));
        _buf = new NativeBuffer(byteCapacity);
        _count = 0;
    }

    ~LargeBufferWriter()
    {
        _buf.Dispose();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _buf.Dispose();
        _count = 0;
    }

    public T* GetWrittenBufffer(out nuint count)
    {
        count = _count;
        return (T*)_buf.Ptr;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T* GetBufferToWrite(nuint count)
    {
        if(count > Capacity - _count) {
            ResizeBuffer(Capacity + count);
            Debug.Assert(count <= Capacity - _count);
        }
        return (T*)_buf.Ptr + _count;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]  // uncommon path, no inlining
    private void ResizeBuffer(nuint minCapacity)
    {
        nuint minByteCapacity = minCapacity * (nuint)sizeof(T);
        nuint availableMaxByteLength = nuint.MaxValue - nuint.MaxValue % (nuint)sizeof(T);

        if(_buf.ByteLength == availableMaxByteLength) {
            throw new InvalidOperationException("cannot write any more.");
        }
        nuint newByteCapacity;
        if(_buf.ByteLength >= availableMaxByteLength / 2) {
            newByteCapacity = availableMaxByteLength;
        }
        else {
            newByteCapacity = Math.Max(Math.Max(4, minByteCapacity), _buf.ByteLength * 2);
        }
        if(newByteCapacity < minByteCapacity) {
            throw new ArgumentOutOfRangeException("Required capacity is too large.");
        }

        var newBuf = new NativeBuffer(newByteCapacity);
        try {
            System.Buffer.MemoryCopy(_buf.Ptr, newBuf.Ptr, newBuf.ByteLength, _count * (nuint)sizeof(T));
        }
        catch {
            newBuf.Dispose();
            throw;
        }
        _buf.Dispose();
        _buf = newBuf;
    }
}
