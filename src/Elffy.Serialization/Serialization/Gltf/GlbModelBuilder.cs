#nullable enable
using Cysharp.Threading.Tasks;
using Elffy.Effective;
using Elffy.Serialization.Gltf.Internal;
using Elffy.Serialization.Gltf.Parsing;
using Elffy.Shapes;
using Elffy.Threading;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Threading;
using G = Elffy.Serialization.Gltf.Parsing;

using ImageData = Elffy.Imaging.Image;
using ImageDataType = Elffy.Imaging.ImageType;
using ReadOnlyImageRef = Elffy.Imaging.ReadOnlyImageRef;

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

        using var glb = GltfParser.ParseGlb(file, ct);

        var layer = model.Layer;
        Debug.Assert(layer is not null);
        using(var operations = new ParallelOperation()) {
            var builderState = new BuilderState(glb, layer, screen, operations);
            BuildRoot(in builderState, model, load);
            await operations.WhenAll();
        }

        await screen.Timings.Update.NextOrNow(ct);
    }

    private static void BuildRoot(in BuilderState state, Model3D model, Model3DLoadMeshDelegate load)
    {
        var gltf = state.Gltf;
        ValidateVersion(gltf);
        if(gltf.scene.TryGetValue(out var sceneNum)) {
            ref readonly var defaultScene = ref GetItemOrThrow(gltf.scenes, sceneNum);
            if(defaultScene.nodes != null) {
                foreach(var nodeNum in defaultScene.nodes) {
                    ref readonly var node = ref GetItemOrThrow(gltf.nodes, nodeNum);
                    BuildNode(in state, in node, model);
                }
            }
        }
    }

    private static void ValidateVersion(GltfObject gltf)
    {
        var version = gltf.asset.version.AsSpan();
        if(version.SequenceEqual(stackalloc byte[] { (byte)'2', (byte)'.', (byte)'0' }) == false) {
            throw new NotSupportedException("only supports gltf v2.0");
        }
    }

    private static void BuildNode(in BuilderState state, in Node node, Positionable parent)
    {
        var gltf = state.Gltf;

        // glTF and Engine has same coordinate (Y-up, right-hand)
        var nodePart = new GlbModelPart();
        nodePart.Name = node.name?.ToString();
        nodePart.Rotation = new Quaternion(node.rotation.X, node.rotation.Y, node.rotation.Z, node.rotation.W);
        nodePart.Position = new Vector3(node.translation.X, node.translation.Y, node.translation.Z);
        nodePart.Scale = new Vector3(node.scale.X, node.scale.Y, node.scale.Z);
        var matrix = new Matrix4(node.matrix.AsSpan());

        state.Tasks.Add(MakeTree(nodePart, parent, state.Layer));

        if(node.mesh.TryGetValue(out var meshNum)) {
            ref readonly var mesh = ref GetItemOrThrow(gltf.meshes, meshNum);
            foreach(ref readonly var meshPrimitive in mesh.primitives.AsSpan()) {
                BuildMeshPrimitive<TangentVertex>(in state, in meshPrimitive, nodePart);
            }
        }

        if(node.children != null) {
            foreach(var childNodeNum in node.children) {
                ref readonly var childNode = ref GetItemOrThrow(gltf.nodes, childNodeNum);
                BuildNode(in state, in childNode, nodePart);
            }
        }
    }

    private static async UniTask MakeTree(GlbModelPart obj, Positionable parent, ObjectLayer layer)
    {
        var screen = layer.GetValidScreen();
        await screen.Timings.Update.Next();
        await obj.Activate(layer);
        parent.Children.Add(obj);
    }

    private unsafe static void StoreIndices(in BufferData data, uint* dest)
    {
        var elementCount = data.Count;
        switch(data.ComponentType) {
            case AccessorComponentType.UnsignedByte: {
                ConvertUInt8ToUInt32((byte*)data.Ptr, dest, elementCount);
                break;
            }
            case AccessorComponentType.UnsignedShort: {
                ConvertUInt16ToUInt32((ushort*)data.Ptr, dest, elementCount);
                break;
            }
            case AccessorComponentType.UnsignedInt: {
                System.Buffer.MemoryCopy(data.Ptr, dest, data.ByteLength, data.ByteLength);
                if(BitConverter.IsLittleEndian == false) {
                    ReverseEndianUInt32(dest, elementCount);
                }
                break;
            }
            default: {
                Debug.Fail("It should not be possible to reach here.");
                break;
            }

        }
    }

    private unsafe static void BuildMeshPrimitive<TVertex>(in BuilderState state, in MeshPrimitive meshPrimitive, Positionable parent) where TVertex : unmanaged
    {
        var gltf = state.Gltf;
        var meshPrimitivePart = new GlbModelPart<TVertex>();
        meshPrimitivePart.Name = "mesh.primitive";
        state.Tasks.Add(MakeTree(meshPrimitivePart, parent, state.Layer));

        if(meshPrimitive.material.TryGetValue(out var materialNum)) {
            ref readonly var material = ref GetItemOrThrow(gltf.materials, materialNum);
            var result = ReadMaterial(in state, in material, meshPrimitivePart);
        }

        var mode = meshPrimitive.mode;
        if(mode != MeshPrimitiveMode.Triangles) {
            throw new NotImplementedException();
        }
        ref readonly var attrs = ref meshPrimitive.attributes;

        // position
        var verticesOutput = meshPrimitivePart.GetVerticesWriter();
        if(attrs.POSITION.TryGetValue(out var posAttr)) {
            ref readonly var position = ref GetItemOrThrow(gltf.accessors, posAttr);
            if(position is not { type: AccessorType.Vec3, componentType: AccessorComponentType.Float }) {
                ThrowInvalidGlb();
            }
            AccessData(in state, in position, verticesOutput, BufferWriteDestinationMode.AllocateNew, &GlbVertexWriter<TVertex>.StorePositions);
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
            AccessData(in state, in normal, verticesOutput, BufferWriteDestinationMode.ExistingMemory, &GlbVertexWriter<TVertex>.StoreNormals);
        }

        // uv
        if(attrs.TEXCOORD_0.TryGetValue(out var uv0Attr)) {
            ref readonly var uv0 = ref GetItemOrThrow(gltf.accessors, uv0Attr);
            if(uv0 is not { type: AccessorType.Vec2, componentType: AccessorComponentType.Float }) {
                ThrowInvalidGlb();
            }
            AccessData(in state, in uv0, verticesOutput, BufferWriteDestinationMode.ExistingMemory, &GlbVertexWriter<TVertex>.StoreUVs);
        }

        // tangent
        if(attrs.TANGENT.TryGetValue(out var tangentAttr)) {
            ref readonly var tangent = ref GetItemOrThrow(gltf.accessors, tangentAttr);
            if(tangent is not { type: AccessorType.Vec3, componentType: AccessorComponentType.Float }) {
                ThrowInvalidGlb();
            }
            AccessData(in state, in tangent, verticesOutput, BufferWriteDestinationMode.ExistingMemory, &GlbVertexWriter<TVertex>.StoreTangents);
        }

        // indices
        var indicesOutput = meshPrimitivePart.GetIndicesWriter();
        if(meshPrimitive.indices.TryGetValue(out var indicesNum)) {
            ref readonly var indices = ref GetItemOrThrow(gltf.accessors, indicesNum);
            if(indices is not
                {
                    type: AccessorType.Scalar,
                    componentType: AccessorComponentType.UnsignedByte or AccessorComponentType.UnsignedShort or AccessorComponentType.UnsignedInt
                }) {
                ThrowInvalidGlb();
            }
            AccessData(in state, indices, indicesOutput, BufferWriteDestinationMode.AllocateNewWithoutInit, &StoreIndices);
        }

        // Calculate tangent
        var needToCalcTangent =
            attrs.POSITION.HasValue &&
            attrs.NORMAL.HasValue &&
            attrs.TEXCOORD_0.HasValue &&
            attrs.TANGENT.HasValue == false &&
            VertexMarshalHelper.GetVertexTypeData<TVertex>().HasField(VertexSpecialField.Tangent);
        if(needToCalcTangent) {
            TVertex* vertices = verticesOutput.GetWrittenBufffer(out var vLength);

            // Length of indices can be 0 in case of non indexed vertices.
            uint* indices = indicesOutput.GetWrittenBufffer(out var iLength);
            GlbVertexWriter<TVertex>.CalcTangents(vertices, vLength, indices, iLength);
        }

        state.Tasks.Add(meshPrimitivePart.GetApplyMeshTask(state.Screen));
    }

    private static TextureConfig ToTextureConfig(in Sampler sampler)
    {
        var config = new TextureConfig
        {
            WrapModeX = sampler.wrapS switch
            {
                SamplerWrap.Repeat => TextureWrapMode.Repeat,
                SamplerWrap.MirroredRepeat => TextureWrapMode.MirroredRepeat,
                SamplerWrap.ClampToEdge => TextureWrapMode.ClampToEdge,
                _ => default,
            },
            WrapModeY = sampler.wrapT switch
            {
                SamplerWrap.Repeat => TextureWrapMode.Repeat,
                SamplerWrap.MirroredRepeat => TextureWrapMode.MirroredRepeat,
                SamplerWrap.ClampToEdge => TextureWrapMode.ClampToEdge,
                _ => default,
            },
            ExpansionMode = sampler.magFilter switch
            {
                SamplerMagFilter.Nearest => TextureExpansionMode.NearestNeighbor,
                SamplerMagFilter.Linear => TextureExpansionMode.Bilinear,
                _ => default,
            },
            MipmapMode = sampler.minFilter switch
            {
                SamplerMinFilter.Nearest or SamplerMinFilter.Linear => TextureMipmapMode.None,
                SamplerMinFilter.NearestMipmapNearest or SamplerMinFilter.NearestMipmapLinear => TextureMipmapMode.NearestNeighbor,
                SamplerMinFilter.LinearMipmapNearest or SamplerMinFilter.LinearMipmapLinear => TextureMipmapMode.Bilinear,
                _ => default,
            },
            ShrinkMode = sampler.minFilter switch
            {
                SamplerMinFilter.Nearest or SamplerMinFilter.NearestMipmapNearest or SamplerMinFilter.LinearMipmapNearest => TextureShrinkMode.NearestNeighbor,
                SamplerMinFilter.Linear or SamplerMinFilter.NearestMipmapLinear or SamplerMinFilter.LinearMipmapLinear => TextureShrinkMode.Bilinear,
                _ => default,
            },
        };
        return config;
    }

    private delegate void LoadTextureAction<T>(GlbModelPart obj, ReadOnlyImageRef imageData, TextureConfig config, T arg);

    private static UniTask LoadTextureTask<T>(IHostScreen screen, GlbModelPart obj, ref ImageData imageData, TextureConfig config, T arg, LoadTextureAction<T> action)
    {
        (imageData, var tmp) = (default, imageData);
        return Task(screen, obj, tmp, config, arg, action);

        static async UniTask Task(IHostScreen screen, GlbModelPart obj, ImageData imageData, TextureConfig config, T arg, LoadTextureAction<T> action)
        {
            try {
                await screen.Timings.Update.Next();
                action.Invoke(obj, imageData, config, arg);
            }
            finally {
                imageData.Dispose();
            }
        }
    }

    private static ReadMaterialResult ReadMaterial(in BuilderState state, in Material material, GlbModelPart obj)
    {
        var gltf = state.Gltf;
        var screen = state.Screen;
        bool hasPbrBaseColorTex = false;
        bool hasPbrMetallicRoughnessTex = false;
        bool hasNormalTex = false;
        bool hasEmissiveTex = false;
        bool hasOcclusionTex = false;

        var shader = new GlbShader();
        obj.Shader = shader;

        if(material.pbrMetallicRoughness != null) {
            var pbr = material.pbrMetallicRoughness.Value;
            shader.MetallicFactor = pbr.metallicFactor;
            shader.BaseColorFactor = new Color4(pbr.baseColorFactor.X, pbr.baseColorFactor.Y, pbr.baseColorFactor.Z, pbr.baseColorFactor.W);
            shader.RoughnessFactor = pbr.roughnessFactor;

            // pbr basecolor
            if(pbr.baseColorTexture.TryGetValue(out var baseColorTexInfo)) {
                ref readonly var texture = ref GetItemOrThrow(gltf.textures, baseColorTexInfo.index);
                if(TryReadTexture(in state, in texture, out var imageData, out var texConfig)) {
                    var loadTexTask = LoadTextureTask(screen, obj, ref imageData, texConfig, shader,
                        static (obj, imageData, texConfig, shader) =>
                        {
                            shader.SetBaseColorTexture(imageData, texConfig);
                        });
                    state.Tasks.Add(loadTexTask);
                }
            }

            // pbr metallic and roughness
            if(pbr.metallicRoughnessTexture.TryGetValue(out var metallicRoughnessTexInfo)) {
                ref readonly var texture = ref GetItemOrThrow(gltf.textures, metallicRoughnessTexInfo.index);
                if(TryReadTexture(in state, in texture, out var imageData, out var texConfig)) {
                    var loadTexTask = LoadTextureTask(screen, obj, ref imageData, texConfig, shader,
                        static (obj, imageData, texConfig, shader) =>
                        {
                            shader.SetMetallicRoughnessTexture(imageData, texConfig);
                        });
                    state.Tasks.Add(loadTexTask);
                }
            }
        }

        // normal texture
        if(material.normalTexture.TryGetValue(out var normalTexInfo)) {
            ref readonly var texture = ref GetItemOrThrow(gltf.textures, normalTexInfo.index);
            var scale = normalTexInfo.scale;
            var uvN = normalTexInfo.texCoord;   // texcoord_n
            if(TryReadTexture(in state, in texture, out var imageData, out var texConfig)) {
                var loadTexTask = LoadTextureTask(screen, obj, ref imageData, texConfig, shader,
                    static (obj, imageData, texConfig, shader) =>
                    {
                        // TODO:
                        shader.SetNormalTexture(imageData, texConfig);
                    });
                hasNormalTex = true;
                state.Tasks.Add(loadTexTask);
            }
        }

        // emissive texture
        if(material.emissiveTexture.TryGetValue(out var emissiveTextureInfo)) {
            ref readonly var texture = ref GetItemOrThrow(gltf.textures, emissiveTextureInfo.index);
            var uvN = emissiveTextureInfo.texCoord; // texcoord_n
            if(TryReadTexture(in state, in texture, out var imageData, out var texConfig)) {
                var loadTexTask = LoadTextureTask(screen, obj, ref imageData, texConfig, shader,
                    static (obj, imageData, texConfig, shader) =>
                    {
                        // TODO:
                    });
                state.Tasks.Add(loadTexTask);
            }
        }

        // occlusion texture
        if(material.occlusionTexture.TryGetValue(out var occlusionTextureInfo)) {
            ref readonly var texture = ref GetItemOrThrow(gltf.textures, occlusionTextureInfo.index);
            var uvN = occlusionTextureInfo.texCoord; // texcoord_n
            var strength = occlusionTextureInfo.strength;
            if(TryReadTexture(in state, in texture, out var imageData, out var texConfig)) {
                var loadTexTask = LoadTextureTask(screen, obj, ref imageData, texConfig, shader,
                    static (obj, imageData, texConfig, shader) =>
                    {
                        // TODO:
                    });
            }
        }

        return new()
        {
            HasPbrBaseColorTex = hasPbrBaseColorTex,
            HasPbrMetallicRoughnessTex = hasPbrMetallicRoughnessTex,
            HasNormalTex = hasNormalTex,
            HasEmissiveTex = hasEmissiveTex,
            HasOcclusionTex = hasOcclusionTex,
        };
    }

    private static bool TryReadTexture(in BuilderState state, in G.Texture texture, out ImageData imageData, out TextureConfig config)
    {
        var gltf = state.Gltf;
        if(texture.source.TryGetValue(out var imageNum)) {
            ref readonly var image = ref GetItemOrThrow(gltf.images, imageNum);
            if(texture.sampler.TryGetValue(out var samplerNum)) {
                ref readonly var sampler = ref GetItemOrThrow(gltf.samplers, samplerNum);
                config = ToTextureConfig(sampler);
            }
            else {
                config = TextureConfig.Default;
            }
            imageData = ReadImage(in state, in image);
            return true;
        }
        imageData = default;
        config = default;
        return false;
    }

    private enum BufferWriteDestinationMode
    {
        AllocateNew,
        AllocateNewWithoutInit,
        ExistingMemory,
    }

    private unsafe static void AccessData<T>(
        in BuilderState state,
        in Accessor accessor,
        ILargeBufferWriter<T> output,
        BufferWriteDestinationMode destMode,
        delegate*<in BufferData, T*, void> callback
    ) where T : unmanaged
    {
        var gltf = state.Gltf;
        if(accessor.bufferView.TryGetValue(out var bufferViewNum) == false) {
            return;
        }
        ref readonly var bufferView = ref GetItemOrThrow(gltf.bufferViews, bufferViewNum);
        var bin = ReadBufferView(in state, in bufferView);
        var data = new BufferData
        {
            P = (IntPtr)bin.Ptr,
            ByteLength = bin.ByteLength,
            ByteStride = bufferView.byteStride,
            Count = accessor.count,
            Type = accessor.type,
            ComponentType = accessor.componentType,
        };

        var elementCount = accessor.count;
        switch(destMode) {
            case BufferWriteDestinationMode.AllocateNew:
            case BufferWriteDestinationMode.AllocateNewWithoutInit: {
                var zeroClear = destMode == BufferWriteDestinationMode.AllocateNew;
                var dest = output.GetBufferToWrite(elementCount, zeroClear);
                callback(in data, dest);
                output.Advance(elementCount);
                break;
            }
            case BufferWriteDestinationMode.ExistingMemory: {
                var dest = output.GetWrittenBufffer(out var writtenCount);
                Debug.Assert(writtenCount >= elementCount);
                callback(in data, dest);
                break;
            }
            default:
                break;
        }
    }

    private unsafe static GlbBinaryData ReadBufferView(in BuilderState state, in BufferView bufferView)
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

    private unsafe static ImageData ReadImage(in BuilderState state, in Image image)
    {
        var gltf = state.Gltf;
        if(image.uri != null) {
            throw new NotSupportedException();
        }

        if(image.bufferView.TryGetValue(out var bufferViewNum) == false) {
            ThrowInvalidGlb();
        }
        ref readonly var bufferView = ref GetItemOrThrow(gltf.bufferViews, bufferViewNum);
        var bin = ReadBufferView(in state, in bufferView);
        if(image.mimeType.TryGetValue(out var mimeType) == false) {
            ThrowInvalidGlb();
        }
        using var stream = new PointerMemoryStream(bin.Ptr, bin.ByteLength);

        return mimeType switch
        {
            ImageMimeType.ImageJpeg => ImageData.FromStream(stream, ImageDataType.Jpg),
            ImageMimeType.ImagePng => ImageData.FromStream(stream, ImageDataType.Png),
            _ => default,
        };
    }

    private record struct BufferData(
        IntPtr P,
        nuint ByteLength,
        nuint? ByteStride,
        nuint Count,
        AccessorType Type,
        AccessorComponentType ComponentType
    )
    {
        public unsafe void* Ptr => (void*)P;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ref T GetItemOrThrow<T>(T[]? array, uint index)
    {
        if(array == null) {
            ThrowInvalidGlb();
        }
        return ref array[index];
    }

    private unsafe static void ReverseEndianUInt32(uint* p, nuint count)
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

    private record struct BuilderState(
        GlbObject Glb,
        ObjectLayer Layer,
        IHostScreen Screen,
        ParallelOperation Tasks
    )
    {
        public readonly GltfObject Gltf => Glb.Gltf;
    }

    private record struct ReadMaterialResult(
        bool HasPbrBaseColorTex,
        bool HasPbrMetallicRoughnessTex,
        bool HasNormalTex,
        bool HasEmissiveTex,
        bool HasOcclusionTex
    );

    private static class GlbVertexWriter<TVertex> where TVertex : unmanaged
    {
        public unsafe static void StorePositions(in BufferData data, TVertex* dest)
        {
            Write<Vector3>(in data, dest, VertexSpecialField.Position);
        }

        public unsafe static void StoreNormals(in BufferData data, TVertex* dest)
        {
            Write<Vector3>(in data, dest, VertexSpecialField.Normal);
        }

        public unsafe static void StoreUVs(in BufferData data, TVertex* dest)
        {
            Write<Vector2>(in data, dest, VertexSpecialField.UV);
        }

        public unsafe static void StoreTangents(in BufferData data, TVertex* dest)
        {
            Write<Vector3>(in data, dest, VertexSpecialField.Tangent);
        }

        private unsafe static void Write<TData>(in BufferData data, TVertex* dest, VertexSpecialField field) where TData : unmanaged
        {
            var vtype = VertexMarshalHelper.GetVertexTypeData<TVertex>();
            if(vtype.TryGetFieldAccessor<TData>(field, out var fieldAccessor) == false) {
                return;
            }

            if(BitConverter.IsLittleEndian == false) {
                throw new PlatformNotSupportedException("Big endian environment is not supported.");
            }
            var ptr = (byte*)data.Ptr;
            var byteStride = data.ByteStride ?? (nuint)sizeof(TData);
            for(nuint i = 0; i < data.Count; i++) {
                fieldAccessor.Field(dest[i]) = *(TData*)(ptr + byteStride * i);
            }
        }

        public unsafe static void CalcTangents(TVertex* vertices, nuint vLength, uint* indices, nuint iLength)
        {
            var vtype = VertexMarshalHelper.GetVertexTypeData<TVertex>();
            if(vtype.TryGetFieldAccessor<Vector3>(VertexSpecialField.Position, out var posField) == false) {
                return;
            }
            if(vtype.TryGetFieldAccessor<Vector2>(VertexSpecialField.UV, out var uvField) == false) {
                return;
            }
            if(vtype.TryGetFieldAccessor<Vector3>(VertexSpecialField.Tangent, out var tangentField) == false) {
                return;
            }

            if(iLength == 0) {
                // non indexed vertices

                for(nuint i = 0; i < vLength / 3; i++) {
                    var i0 = i * 3;
                    var i1 = i * 3 + 1;
                    var i2 = i * 3 + 2;
                    var tangent = CalcTangent(
                        posField.Field(vertices[i0]),
                        posField.Field(vertices[i1]),
                        posField.Field(vertices[i2]),
                        uvField.Field(vertices[i0]),
                        uvField.Field(vertices[i1]),
                        uvField.Field(vertices[i2])).Normalized();
                    tangentField.Field(vertices[i0]) = tangent;
                    tangentField.Field(vertices[i1]) = tangent;
                    tangentField.Field(vertices[i2]) = tangent;
                }
            }
            else {
                // indexed vertices

                for(nuint i = 0; i < iLength / 3; i++) {
                    var i0 = indices[i * 3];
                    var i1 = indices[i * 3 + 1];
                    var i2 = indices[i * 3 + 2];
                    if(i0 >= vLength) { ThrowIndexOutOfRange(nameof(vertices), i0, vLength); }
                    if(i1 >= vLength) { ThrowIndexOutOfRange(nameof(vertices), i1, vLength); }
                    if(i2 >= vLength) { ThrowIndexOutOfRange(nameof(vertices), i2, vLength); }
                    var tangent = CalcTangent(
                        posField.Field(vertices[i0]),
                        posField.Field(vertices[i1]),
                        posField.Field(vertices[i2]),
                        uvField.Field(vertices[i0]),
                        uvField.Field(vertices[i1]),
                        uvField.Field(vertices[i2])
                    );
                    tangentField.Field(vertices[i0]) += tangent;
                    tangentField.Field(vertices[i1]) += tangent;
                    tangentField.Field(vertices[i2]) += tangent;
                }
                for(nuint i = 0; i < vLength; i++) {
                    tangentField.Field(vertices[i]).Normalize();
                }
            }
            return;

            static Vector3 CalcTangent(in Vector3 pos0, in Vector3 pos1, in Vector3 pos2, in Vector2 uv0, in Vector2 uv1, in Vector2 uv2)
            {
                var deltaUV1 = uv1 - uv0;
                var deltaUV2 = uv2 - uv0;
                var deltaPos1 = pos1 - pos0;
                var deltaPos2 = pos2 - pos0;
                var d = 1f / (deltaUV1.X * deltaUV2.Y - deltaUV1.Y * deltaUV2.X);
                var tangent = d * (deltaUV2.Y * deltaPos1 - deltaUV1.Y * deltaPos2);
#if DEBUG
                var bitangent = d * (deltaUV1.X * deltaPos2 - deltaUV2.X * deltaPos1);
#endif
                return tangent;
            }

            [DoesNotReturn]
            static void ThrowIndexOutOfRange(string name, nuint index, nuint len) =>
                throw new IndexOutOfRangeException($"Index was outside the bounds of the array. (index: {index}, {name}.Length: {len})");
        }
    }
}
