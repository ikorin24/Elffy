#nullable enable
using Cysharp.Threading.Tasks;
using Elffy.Effective;
using Elffy.Effective.Unsafes;
using Elffy.Shapes;
using System;
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

        using var verticesBuffer = new UnsafeBufferWriter<Vertex>();
        using var indicesBuffer = new UnsafeBufferWriter<int>();
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

    private static void BuildRoot(UnsafeBufferWriter<Vertex> verticesOutput, UnsafeBufferWriter<int> indicesOutput, GlbObject glb, Model3D model, Model3DLoadMeshDelegate load)
    {
        var gltf = glb.Gltf;

        ValidateVersion(gltf);

        if(gltf.scene.TryGetValue(out var sceneNum)) {
            var accessors = gltf.accessors;
            var bufferViews = gltf.bufferViews;
            var buffers = gltf.buffers;

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
                                        if(indices.bufferView.TryGetValue(out var bufferViewNum)) {
                                            ref readonly var bufferView = ref GetItemOrThrow(gltf.bufferViews, bufferViewNum);
                                            ref readonly var buffer = ref GetItemOrThrow(gltf.buffers, bufferView.buffer);
                                            if(buffer.uri == null) {
                                                if(bufferView.byteStride.TryGetValue(out var stride)) {
                                                    throw new NotImplementedException();
                                                }
                                                else {
                                                    var offset = (nuint)bufferView.byteOffset;
                                                    var len = (nuint)bufferView.byteLength;
                                                    var bin = glb.GetBinaryData(bufferView.buffer).Slice(offset, len);

                                                    if(bin.Length > int.MaxValue) {
                                                        throw new NotSupportedException();
                                                    }
                                                    if(BitConverter.IsLittleEndian == false) {
                                                        throw new PlatformNotSupportedException("only for little endian runtime.");
                                                    }

                                                    switch(indices.componentType) {
                                                        case AccessorComponentType.UnsignedByte: {
                                                            int l = (int)(bin.Length);
                                                            var dest = indicesOutput.GetSpan(l).Slice(0, l);
                                                            unsafe {
                                                                byte* binPtr = bin.Ptr;
                                                                // TODO: use SIMD
                                                                for(int i = 0; i < l; i++) {
                                                                    // cast uint8 to int32
                                                                    dest.At(i) = binPtr[i];
                                                                }
                                                            }
                                                            indicesOutput.Advance(l);
                                                            break;
                                                        }
                                                        case AccessorComponentType.UnsignedShort: {
                                                            nuint l = bin.Length / sizeof(ushort);
                                                            var dest = indicesOutput.GetSpan((int)l).Slice(0, (int)l);
                                                            unsafe {
                                                                fixed(int* d = dest) {
                                                                    ConvertUInt16ToUInt32((ushort*)bin.Ptr, (uint*)d, l);
                                                                }
                                                            }
                                                            indicesOutput.Advance((int)l);
                                                            break;
                                                        }
                                                        case AccessorComponentType.UnsignedInt: {
                                                            var l = (int)(bin.Length / sizeof(uint));
                                                            var dest = indicesOutput.GetSpan(l).Slice(0, l).MarshalCast<int, byte>();
                                                            bin.CopyTo(dest);
                                                            indicesOutput.Advance(l);
                                                            break;
                                                        }
                                                        default: {
                                                            ThrowInvalidGlb();
                                                            break;
                                                        }

                                                    }
                                                }
                                            }
                                            else {
                                                throw new NotImplementedException();
                                            }
                                        }
                                    }
                                    else {
                                        if(attrs.POSITION.TryGetValue(out var positionNum)) {
                                            ref readonly var position = ref GetItemOrThrow(accessors, positionNum);
                                            if(position.bufferView.TryGetValue(out var bufferViewNum)) {

                                            }
                                        }
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ref T GetItemOrThrow<T>(T[]? array, int index)
    {
        if(array == null) {
            ThrowInvalidGlb();
        }
        return ref array[index];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static T AsNotNull<T>(T? obj) where T : class
    {
        if(obj is null) {
            throw new FormatException("invalid glb");
        }
        return obj;
    }

    [DoesNotReturn]
    private static void ThrowInvalidGlb() => throw new FormatException("invalid glb");

    //private static unsafe void ConvertUInt16ToUInt32___(ushort* src, uint* dest, nuint elementCount)
    //{
    //    if(Avx2.IsSupported) {
    //        var (n, m) = Math.DivRem(elementCount, 8);
    //        for(nuint i = 0; i < n; i++) {
    //            Unsafe.As<uint, Vector256<int>>(ref dest[i * 8]) =
    //                Avx2.ConvertToVector256Int32(Avx2.LoadVector128(src + i * 8));
    //        }
    //        var offset = n * 8;
    //        for(nuint i = 0; i < m; i++) {
    //            dest[offset + i] = (uint)src[offset + i];
    //        }
    //    }
    //    else {
    //        NonVectorFallback(src, dest, elementCount);
    //    }

    //    static void NonVectorFallback(ushort* src, uint* dest, nuint elementCount)
    //    {
    //        for(nuint i = 0; i < elementCount; i++) {
    //            dest[i] = (uint)src[i];
    //        }
    //    }
    //}

    private static unsafe void ConvertUInt16ToUInt32(ushort* src, uint* dest, nuint elementCount)
    {
        if(Avx2.IsSupported) {
            var (n, m) = Math.DivRem(elementCount, 8);

            const uint LoopUnrollFactor = 4;
            var (n1, n2) = Math.DivRem(n, LoopUnrollFactor);
            for(nuint i = 0; i < n1; i++) {
                var x = i * 8 * LoopUnrollFactor;
                Unsafe.As<uint, Vector256<int>>(ref dest[x]) = Avx2.ConvertToVector256Int32(Avx2.LoadVector128(&src[x]));
                Unsafe.As<uint, Vector256<int>>(ref dest[x + 8]) = Avx2.ConvertToVector256Int32(Avx2.LoadVector128(&src[x + 8]));
                Unsafe.As<uint, Vector256<int>>(ref dest[x + 16]) = Avx2.ConvertToVector256Int32(Avx2.LoadVector128(&src[x + 16]));
                Unsafe.As<uint, Vector256<int>>(ref dest[x + 24]) = Avx2.ConvertToVector256Int32(Avx2.LoadVector128(&src[x + 24]));
            }
            var offset = n1 * 8 * LoopUnrollFactor;
            for(nuint i = 0; i < n2; i++) {
                var x = offset + i * 8;
                Unsafe.As<uint, Vector256<int>>(ref dest[x]) = Avx2.ConvertToVector256Int32(Avx2.LoadVector128(&src[x]));
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
}
