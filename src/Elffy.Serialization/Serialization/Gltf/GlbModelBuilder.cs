#nullable enable
using Cysharp.Threading.Tasks;
using Elffy.Effective;
using Elffy.Effective.Unsafes;
using Elffy.Serialization.Gltf.Internal;
using Elffy.Shapes;
using Elffy.Threading;
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
        using var verticesBuffer = new LargeBufferWriter<Vertex>();
        using var indicesBuffer = new LargeBufferWriter<uint>();

        var builderState = new BuilderState(glb, verticesBuffer, indicesBuffer);

        await BuildRoot(in builderState, model, load);

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

    private static UniTask BuildRoot(in BuilderState state, Model3D model, Model3DLoadMeshDelegate load)
    {
        var gltf = state.Gltf;
        ValidateVersion(gltf);
        if(gltf.scene.TryGetValue(out var sceneNum) == false) {
            return UniTask.CompletedTask;
        }
        ref readonly var defaultScene = ref GetItemOrThrow(gltf.scenes, sceneNum);
        if(defaultScene.nodes == null) {
            return UniTask.CompletedTask;
        }
        using var operations = new ParallelOperation();

        foreach(var nodeNum in defaultScene.nodes) {
            ref readonly var node = ref GetItemOrThrow(gltf.nodes, nodeNum);
            if(node.mesh.TryGetValue(out var meshNum) == false) {
                continue;
            }

            var part = new GlbModelPart();

            // glTF and Engine has same coordinate (Y-up, right-hand)
            part.Rotation = UnsafeEx.As<Quaternion, Elffy.Quaternion>(in node.rotation);
            part.Position = UnsafeEx.As<Vector3, Elffy.Vector3>(in node.translation);
            part.Scale = UnsafeEx.As<Vector3, Elffy.Vector3>(in node.scale);

            operations.Add(MakeTree(part, model));

            ref readonly var mesh = ref GetItemOrThrow(gltf.meshes, meshNum);
            foreach(ref readonly var meshPrimitive in mesh.primitives.AsSpan()) {
                ReadMeshPrimitive(in state, in meshPrimitive);
            }
        }

        return operations.WhenAll();

        static async UniTask MakeTree(Positionable obj, Positionable parent)
        {
            var layer = parent.Layer as WorldLayer;
            Debug.Assert(layer != null);
            var screen = parent.GetValidScreen();
            await screen.Timings.Update.Next();

            // TODO: 
            //await obj.Activate(layer);
            //parent.Children.Add(obj);
        }
    }

    private static void ValidateVersion(GltfObject gltf)
    {
        var version = gltf.asset.version.AsSpan();
        if(version.SequenceEqual(stackalloc byte[] { (byte)'2', (byte)'.', (byte)'0' }) == false) {
            throw new NotSupportedException("only supports gltf v2.0");
        }
    }

    private unsafe static readonly AccessBufferAction _storePositions = StorePositions;
    private unsafe static readonly AccessBufferAction _storeNormals = StoreNormals;
    private unsafe static readonly AccessBufferAction _storeUVs = StoreUVs;
    private unsafe static readonly AccessBufferAction _storeIndices = StoreIndices;

    private unsafe static void StorePositions(in BuilderState state, in BufferData data)
    {
        Debug.Assert(data.ComponentType is AccessorComponentType.Float);

        var output = state.VerticesOutput;
        nuint elementCount = data.ElementCount;
        Vertex* dest = output.GetBufferToWrite(elementCount, true);
        if(BitConverter.IsLittleEndian == false) {
            throw new PlatformNotSupportedException("Big endian environment is not supported.");
        }
        var ptr = data.Ptr;
        for(nuint i = 0; i < elementCount / 3; i++) {
            dest[i].Position = new()
            {
                X = ((float*)ptr)[i * 3],
                Y = ((float*)ptr)[i * 3 + 1],
                Z = ((float*)ptr)[i * 3 + 2],
            };
        }
        output.Advance(elementCount);
    }

    private unsafe static void StoreNormals(in BuilderState state, in BufferData data)
    {
        Debug.Assert(data.ComponentType is AccessorComponentType.Float);

        var output = state.VerticesOutput;
        nuint elementCount = data.ElementCount;
        Vertex* dest = output.GetWrittenBufffer(out var writtenCount);
        Debug.Assert(writtenCount >= elementCount);
        if(BitConverter.IsLittleEndian == false) {
            throw new PlatformNotSupportedException("Big endian environment is not supported.");
        }
        var ptr = data.Ptr;
        for(nuint i = 0; i < elementCount / 3; i++) {
            dest[i].Normal = new()
            {
                X = ((float*)ptr)[i * 3],
                Y = ((float*)ptr)[i * 3 + 1],
                Z = ((float*)ptr)[i * 3 + 2],
            };
        }
    }

    private unsafe static void StoreUVs(in BuilderState state, in BufferData data)
    {
        Debug.Assert(data.ComponentType is AccessorComponentType.Float);

        var output = state.VerticesOutput;
        nuint elementCount = data.ElementCount;
        Vertex* dest = output.GetWrittenBufffer(out var writtenCount);
        Debug.Assert(writtenCount >= elementCount);
        if(BitConverter.IsLittleEndian == false) {
            throw new PlatformNotSupportedException("Big endian environment is not supported.");
        }
        var ptr = data.Ptr;
        for(nuint i = 0; i < elementCount / 2; i++) {
            dest[i].UV = new()
            {
                X = ((float*)ptr)[i * 2],
                Y = ((float*)ptr)[i * 2 + 1],
            };
        }
    }

    private unsafe static void StoreIndices(in BuilderState state, in BufferData data)
    {
        var output = state.IndicesOutput;
        var elementCount = data.ElementCount;
        switch(data.ComponentType) {
            case AccessorComponentType.UnsignedByte: {
                uint* dest = output.GetBufferToWrite(elementCount);
                ConvertUInt8ToUInt32((byte*)data.Ptr, dest, elementCount);
                output.Advance(elementCount);
                break;
            }
            case AccessorComponentType.UnsignedShort: {
                uint* dest = output.GetBufferToWrite(elementCount);
                ConvertUInt16ToUInt32((ushort*)data.Ptr, dest, elementCount);
                output.Advance(elementCount);
                break;
            }
            case AccessorComponentType.UnsignedInt: {
                uint* dest = output.GetBufferToWrite(elementCount);
                System.Buffer.MemoryCopy(data.Ptr, dest, data.ByteLength, data.ByteLength);
                if(BitConverter.IsLittleEndian == false) {
                    ReverseEndian(dest, elementCount);
                }
                output.Advance(elementCount);
                break;
            }
            default: {
                Debug.Fail("It should not be possible to reach here.");
                break;
            }

        }
    }

    private unsafe static void ReadMeshPrimitive(in BuilderState state, in MeshPrimitive meshPrimitive)
    {
        var gltf = state.Gltf;

        if(meshPrimitive.material.TryGetValue(out var materialNum)) {
            ref readonly var material = ref GetItemOrThrow(gltf.materials, materialNum);
            ReadMaterial(in state, in material);
        }

        switch(meshPrimitive.mode) {
            case MeshPrimitiveMode.Points:
            case MeshPrimitiveMode.Lines:
            case MeshPrimitiveMode.LineLoop:
            case MeshPrimitiveMode.LineStrip: {
                throw new NotImplementedException();
            }
            case MeshPrimitiveMode.Triangles: {
                ref readonly var attrs = ref meshPrimitive.attributes;

                // position
                if(attrs.POSITION.TryGetValue(out var posAttr)) {
                    ref readonly var position = ref GetItemOrThrow(gltf.accessors, posAttr);
                    if(position is not { type: AccessorType.Vec3, componentType: AccessorComponentType.Float }) {
                        ThrowInvalidGlb();
                    }
                    AccessData(in state, in position, _storePositions);
                }
                else {
                    throw new NotSupportedException();
                }

                // normal
                if(attrs.NORMAL.TryGetValue(out var normalAttr)) {
                    ref readonly var normal = ref GetItemOrThrow(gltf.accessors, normalAttr);
                    if(normal is not { type: AccessorType.Vec3, componentType: AccessorComponentType.Float }) {
                        ThrowInvalidGlb();
                    }
                    AccessData(in state, in normal, _storeNormals);
                }

                // uv
                if(attrs.TEXCOORD_0.TryGetValue(out var uv0Attr)) {
                    ref readonly var uv0 = ref GetItemOrThrow(gltf.accessors, uv0Attr);
                    if(uv0 is not { type: AccessorType.Vec2, componentType: AccessorComponentType.Float }) {
                        ThrowInvalidGlb();
                    }
                    AccessData(in state, in uv0, _storeUVs);
                }

                // indices
                if(meshPrimitive.indices.TryGetValue(out var indicesNum)) {
                    ref readonly var indices = ref GetItemOrThrow(gltf.accessors, indicesNum);
                    if(indices is not
                        {
                            type: AccessorType.Scalar,
                            componentType: AccessorComponentType.UnsignedByte or AccessorComponentType.UnsignedShort or AccessorComponentType.UnsignedInt
                        }) {
                        ThrowInvalidGlb();
                    }
                    AccessData(in state, indices, _storeIndices);
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

    private static void ReadMaterial(in BuilderState state, in Material material)
    {
        var gltf = state.Gltf;

        // normal texture
        if(material.normalTexture.TryGetValue(out var normalTexInfo)) {
            ref readonly var texture = ref GetItemOrThrow(gltf.textures, normalTexInfo.index);
            var scale = normalTexInfo.scale;
            var uvN = normalTexInfo.texCoord;   // texcoord_n
            if(texture.source.TryGetValue(out var imageNum)) {
                ref readonly var image = ref GetItemOrThrow(gltf.images, imageNum);
                // TODO:
                var loadedImage = ReadImage(in state, in image);
                loadedImage.Dispose();
            }
            if(texture.sampler.TryGetValue(out var samplerNum)) {
                ref readonly var sampler = ref GetItemOrThrow(gltf.samplers, samplerNum);
            }
        }

        // emissive texture
        if(material.emissiveTexture.TryGetValue(out var emissiveTextureInfo)) {
            ref readonly var texture = ref GetItemOrThrow(gltf.textures, emissiveTextureInfo.index);
            var uvN = emissiveTextureInfo.texCoord; // texcoord_n
            if(texture.source.TryGetValue(out var imageNum)) {
                ref readonly var image = ref GetItemOrThrow(gltf.images, imageNum);
                // TODO:
                var loadedImage = ReadImage(in state, in image);
                loadedImage.Dispose();
            }
            if(texture.sampler.TryGetValue(out var samplerNum)) {
                ref readonly var sampler = ref GetItemOrThrow(gltf.samplers, samplerNum);
            }
        }

        // occlusion texture
        if(material.occlusionTexture.TryGetValue(out var occlusionTextureInfo)) {
            ref readonly var texture = ref GetItemOrThrow(gltf.textures, occlusionTextureInfo.index);
            var uvN = occlusionTextureInfo.texCoord; // texcoord_n
            var strength = occlusionTextureInfo.strength;
            if(texture.source.TryGetValue(out var imageNum)) {
                ref readonly var image = ref GetItemOrThrow(gltf.images, imageNum);
                // TODO:
                var loadedImage = ReadImage(in state, in image);
                loadedImage.Dispose();
            }
            if(texture.sampler.TryGetValue(out var samplerNum)) {
                ref readonly var sampler = ref GetItemOrThrow(gltf.samplers, samplerNum);
            }
        }
    }

    private unsafe static void AccessData(in BuilderState state, in Accessor accessor, AccessBufferAction callback)
    {
        var gltf = state.Gltf;
        if(accessor.bufferView.TryGetValue(out var bufferViewNum) == false) {
            return;
        }
        ref readonly var bufferView = ref GetItemOrThrow(gltf.bufferViews, bufferViewNum);
        var bin = ReadBuffer(in state, in bufferView);
        var data = new BufferData((IntPtr)bin.Ptr, bin.ByteLength, bufferView.byteStride, accessor.componentType);
        callback.Invoke(in state, in data);
    }

    private unsafe static GlbBinaryData ReadBuffer(in BuilderState state, in BufferView bufferView)
    {
        var gltf = state.Gltf;
        ref readonly var buffer = ref GetItemOrThrow(gltf.buffers, bufferView.buffer);
        if(buffer.uri == null) {
            nuint offset = bufferView.byteOffset;
            nuint len = bufferView.byteLength;
            var bin = state.Glb.GetBinaryData(bufferView.buffer).Slice(offset, len);
            return bin;
        }
        else {
            // TODO: fetch data from uri
            throw new NotImplementedException();
        }
    }

    private unsafe static Imaging.Image ReadImage(in BuilderState state, in Image image)
    {
        var gltf = state.Gltf;
        if(image.uri != null) {
            throw new NotSupportedException();
        }

        if(image.bufferView.TryGetValue(out var bufferViewNum) == false) {
            ThrowInvalidGlb();
        }
        ref readonly var bufferView = ref GetItemOrThrow(gltf.bufferViews, bufferViewNum);
        var bin = ReadBuffer(in state, in bufferView);
        if(image.mimeType.TryGetValue(out var mimeType) == false) {
            ThrowInvalidGlb();
        }
        using var stream = new PointerMemoryStream(bin.Ptr, bin.ByteLength);

        return mimeType switch
        {
            ImageMimeType.ImageJpeg => Imaging.Image.FromStream(stream, Imaging.ImageType.Jpg),
            ImageMimeType.ImagePng => Imaging.Image.FromStream(stream, Imaging.ImageType.Png),
            _ => default,
        };
    }

    private record struct BufferData(
        IntPtr P,
        nuint ByteLength,
        nuint? ByteStride,
        AccessorComponentType ComponentType
    )
    {
        public unsafe void* Ptr => (void*)P;

        public nuint ElementCount => ComponentType switch
        {
            AccessorComponentType.Byte or AccessorComponentType.UnsignedByte => ByteLength,
            AccessorComponentType.UnsignedShort or AccessorComponentType.Short => ByteLength / 2,
            AccessorComponentType.UnsignedInt or AccessorComponentType.Float => ByteLength / 4,
            _ => throw new InvalidOperationException("Invalid enum value"),
        };
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

    private unsafe delegate void AccessBufferAction(in BuilderState state, in BufferData data);

    private record struct BuilderState(
        GlbObject Glb,
        LargeBufferWriter<Vertex> VerticesOutput,
        LargeBufferWriter<uint> IndicesOutput
    )
    {
        public readonly GltfObject Gltf => Glb.Gltf;
    }

    private sealed class GlbModelPart : Renderable
    {
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
    public T* GetBufferToWrite(nuint count, bool zeroCleared = false)
    {
        if(count > Capacity - _count) {
            ResizeBuffer(Capacity + count);
            Debug.Assert(count <= Capacity - _count);
        }
        var p = (T*)_buf.Ptr + _count;
        if(zeroCleared) {
            Clear(p, count * (nuint)sizeof(T));

            static void Clear(void* ptr, nuint byteLen)
            {
#if NET7_0_OR_GREATER
                NativeMemory.Clear(ptr, byteLen);
#else
                if(byteLen <= int.MaxValue) {
                    new Span<byte>(ptr, (int)byteLen).Clear();
                }
                else {
                    throw new NotImplementedException();
                }
#endif
            }
        }
        return p;
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
