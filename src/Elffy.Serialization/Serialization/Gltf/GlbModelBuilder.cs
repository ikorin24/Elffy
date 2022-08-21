#nullable enable
using Cysharp.Threading.Tasks;
using Elffy.Effective;
using Elffy.Effective.Unsafes;
using Elffy.Serialization.Gltf.Internal;
using Elffy.Shapes;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
        var version = gltf.asset.version.AsSpan();

        if(version.SequenceEqual(stackalloc byte[] { (byte)'2', (byte)'.', (byte)'0' }) == false) {
            throw new NotSupportedException("only supports gltf v2.0");
        }

        if(TryGetValue(gltf.scene, out var scene)) {
            var scenes = AsNotNull(gltf.scenes);
            var nodes = AsNotNull(gltf.nodes);
            var meshes = AsNotNull(gltf.meshes);
            var accessors = gltf.accessors;
            var bufferViews = gltf.bufferViews;
            var buffers = gltf.buffers;

            ref readonly var defaultScene = ref scenes[scene];
            if(defaultScene.nodes != null) {
                foreach(var nodeNum in defaultScene.nodes) {
                    ref readonly var node = ref nodes[nodeNum];
                    if(TryGetValue(node.mesh, out var meshNum)) {
                        ref readonly var mesh = ref meshes[meshNum];
                        foreach(var meshPrimitive in mesh.primitives) {
                            switch(meshPrimitive.mode) {
                                case MeshPrimitiveMode.Points:
                                case MeshPrimitiveMode.Lines:
                                case MeshPrimitiveMode.LineLoop:
                                case MeshPrimitiveMode.LineStrip: {
                                    throw new NotImplementedException();
                                }
                                case MeshPrimitiveMode.Triangles: {
                                    ref readonly var attrs = ref meshPrimitive.attributes;
                                    if(TryGetValue(meshPrimitive.indices, out var indicesNum)) {
                                        ThrowIfNull(accessors);
                                        ref readonly var indices = ref accessors[indicesNum];
                                        if(indices.type != AccessorType.Scalar) {
                                            ThrowInvalidGlb();
                                        }
                                        if(TryGetValue(indices.bufferView, out var bufferViewNum)) {
                                            ThrowIfNull(bufferViews);
                                            ref readonly var bufferView = ref bufferViews[bufferViewNum];
                                            ThrowIfNull(buffers);
                                            ref readonly var buffer = ref buffers[bufferView.buffer];
                                            if(buffer.uri == null) {
                                                if(TryGetValue(bufferView.byteStride, out var stride)) {
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
                                                            int l = (int)(bin.Length / sizeof(ushort));
                                                            var dest = indicesOutput.GetSpan(l).Slice(0, l);
                                                            unsafe {
                                                                ushort* binPtr = (ushort*)bin.Ptr;
                                                                // TODO: use SIMD
                                                                for(int i = 0; i < l; i++) {
                                                                    // cast uint16 to int32
                                                                    dest.At(i) = binPtr[i];
                                                                }
                                                            }
                                                            indicesOutput.Advance(l);
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
                                        if(TryGetValue(attrs.POSITION, out var positionNum)) {
                                            ThrowIfNull(accessors);
                                            ref readonly var position = ref accessors[positionNum];
                                            if(TryGetValue(position.bufferView, out var bufferViewNum)) {

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

    private static T AsNotNull<T>(T? obj) where T : class
    {
        if(obj is null) {
            throw new FormatException("invalid glb");
        }
        return obj;
    }

    private static void ThrowIfNull([NotNull] object? obj)
    {
        if(obj is null) {
            throw new FormatException("invalid glb");
        }
    }

    [DoesNotReturn]
    private static void ThrowInvalidGlb() => throw new FormatException("invalid glb");

    private static bool TryGetValue<T>(T? input, out T value) where T : struct
    {
        if(input.HasValue) {
            value = input.Value;
            return true;
        }
        value = default;
        return false;
    }
}
