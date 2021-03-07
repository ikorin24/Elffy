#nullable enable
using System;
using System.Threading;
using System.Diagnostics.CodeAnalysis;
using Elffy.Core;
using Elffy.Shapes;
using Elffy.Shading;
using Elffy.Effective.Unsafes;
using Elffy.OpenGL;
using StringLiteral;
using Cysharp.Threading.Tasks;
using FbxTools;

namespace Elffy.Serialization
{
    public static class FbxModelBuilder
    {
        private sealed record StateObject(IResourceLoader ResourceLoader, string Name, CancellationToken CancellationToken);

        /// <summary>Create <see cref="Model3D"/> instance from resource with lazy loading.</summary>
        /// <remarks>Loading will run after <see cref="FrameObject.Activate(Layer)"/> on thread pool.</remarks>
        /// <param name="resourceLoader">resource loader</param>
        /// <param name="name">resource name</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>new <see cref="Model3D"/> instance</returns>
        public static Model3D CreateLazyLoadingFbx(IResourceLoader resourceLoader, string name, CancellationToken cancellationToken = default)
        {
            if(resourceLoader is null) {
                ThrowNullArg();
                [DoesNotReturn] static void ThrowNullArg() => throw new ArgumentNullException(nameof(resourceLoader));
            }
            if(resourceLoader.HasResource(name) == false) {
                ThrowNotFound(name);
                [DoesNotReturn] static void ThrowNotFound(string name) => throw new ResourceNotFoundException(name);
            }

            var obj = new StateObject(resourceLoader, name, cancellationToken);

            return Model3D.Create(obj, Build, RenderModel);
        }

        private static void RenderModel(Model3D model3D, in Matrix4 model, in Matrix4 view, in Matrix4 projection, Model3DDrawElementsDelegate drawElements)
        {
            VAO.Bind(model3D.VAO);
            IBO.Bind(model3D.IBO);
            model3D.ShaderProgram!.Apply(in model, in view, in projection);
            drawElements.Invoke(0, model3D.IBO.Length);
            VAO.Unbind();
            IBO.Unbind();
        }

        private static async UniTask Build(StateObject obj, Model3D model, Model3DLoadMeshDelegate load)
        {
            var (resourceLoader, name, token) = obj;
            token.ThrowIfCancellationRequested();

            await UniTask.SwitchToThreadPool();
            // --------------------------------------
            //      ↓ thread pool

            token.ThrowIfCancellationRequested();

            // Parse fbx file
            using var fbx = FbxParser.Parse(resourceLoader.GetStream(name));
            using var vertices = UnsafeRawList<Vertex>.New();
            using var indices = UnsafeRawList<int>.New();
            BuildCore(fbx, token, vertices, indices);

            //      ↑ thread pool
            // --------------------------------------
            await model.HostScreen.AsyncBack.ToTiming(FrameLoopTiming.Update, token);
            // --------------------------------------
            //      ↓ main thread

            model.Shader = PhongShaderSource.Instance;

            if(model.LifeState == LifeState.Activated || model.LifeState == LifeState.Alive) {
                load.Invoke(vertices.AsSpan(), indices.AsSpan());
            }

            //      ↑ main thread
            // --------------------------------------
            await UniTask.SwitchToThreadPool();
            // --------------------------------------
            //      ↓ thread pool

            // 'using' scope ends here. Dispose resources on a thread pool. 
            // Nobody knows the thread if exceptions are thrown in this method,
            // but I don't care about that.
        }

        private static void BuildCore(FbxObject fbx, CancellationToken cancellationToken, UnsafeRawList<Vertex> vertices, UnsafeRawList<int> indices)
        {
            // --------------------------------------
            //      ↓ thread pool

            // Get "Objects" node
            ref readonly var objects = ref fbx.Find(FbxConsts.Objects());

            Span<int> indexBuffer = stackalloc int[objects.Children.Length];
            var geometryCount = objects.FindIndexAll(FbxConsts.Geometry(), indexBuffer);
            foreach(var i in indexBuffer.Slice(0, geometryCount)) {

                // Get "Objects" -> "Geometry" node
                ref readonly var geometry = ref objects.Children[i];

                // Get geometry data
                GetGeometryData(geometry,
                                out var id,
                                out var geometryName,
                                out var positions,
                                out var indicesRaw,
                                out var normals,
                                out var materials);

                vertices.Capacity += indicesRaw.Length;
                indices.Capacity += indicesRaw.Length;

                // Resolve geometry data into vertices and indices.
                ResolveVertices(indicesRaw, positions, normals, vertices, indices);
                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        private static void GetGeometryData(in FbxNode geometry,
                                            out long id,
                                            out ReadOnlySpan<byte> name,
                                            out ReadOnlySpan<double> positions,
                                            out ReadOnlySpan<int> indicesRaw,
                                            out ReadOnlySpan<double> normals,
                                            out ReadOnlySpan<int> materials)
        {
            id = geometry.Properties[0].AsInt64();
            name = geometry.Properties[1].AsString();

            positions = default;
            indicesRaw = default;
            normals = default;
            materials = default;

            foreach(var node in geometry.Children) {
                var nodeName = node.Name;
                if(nodeName.SequenceEqual(FbxConsts.Vertices())) {
                    positions = node.Properties[0].AsDoubleArray();
                }
                else if(nodeName.SequenceEqual(FbxConsts.Indices())) {
                    indicesRaw = node.Properties[0].AsInt32Array();
                }
                else if(nodeName.SequenceEqual(FbxConsts.NormalInfo())) {
                    normals = node.Find(FbxConsts.Normals()).Properties[0].AsDoubleArray();
                }
                else if(nodeName.SequenceEqual(FbxConsts.MaterialInfo())) {
                    materials = node.Find(FbxConsts.Materials()).Properties[0].AsInt32Array();
                }
            }
        }

        private static void ResolveVertices(ReadOnlySpan<int> indicesRaw,
                                            ReadOnlySpan<double> positions,
                                            ReadOnlySpan<double> normals,
                                            UnsafeRawList<Vertex> verticesMarged,
                                            UnsafeRawList<int> indicesMarged)
        {
            // positions の数は幾何的な頂点の数
            // normals の数は属性が同じ頂点の数 (属性: 座標・法線・頂点色など) (== indices の数)
            // 頂点属性の数になるように、positions を拡張する

            var indicesOffset = indicesMarged.Count;
            int n_gon = 0;
            for(int i = 0; i < indicesRaw.Length; i++) {
                n_gon++;
                var isLast = indicesRaw[i] < 0;
                var index = isLast ? (-indicesRaw[i] - 1) : indicesRaw[i];     // 負のインデックスは多角形ポリゴンの最後の頂点を表す (2の補数が元の値)

                var p = new Vector3((float)positions[index * 3], (float)positions[index * 3 + 1], (float)positions[index * 3 + 2]);
                var normal = new Vector3((float)normals[i * 3], (float)normals[i * 3 + 1], (float)normals[i * 3 + 2]);
                verticesMarged.Add(new Vertex(
                    position: p,
                    normal: normal,
                    uv: default      // TODO:
                ));
                if(isLast) {
                    if(n_gon <= 2) { throw new FormatException(); }
                    for(int n = 0; n < n_gon - 2; n++) {
                        var j = indicesOffset + i - n_gon + 1;
                        indicesMarged.Add(j);
                        indicesMarged.Add(j + n + 1);
                        indicesMarged.Add(j + n + 2);
                    }
                    n_gon = 0;
                }
            }
        }
    }

    internal static partial class FbxConsts
    {
        [Utf8("Objects")]
        public static partial ReadOnlySpan<byte> Objects();

        [Utf8("Geometry")]
        public static partial ReadOnlySpan<byte> Geometry();

        [Utf8("Vertices")]
        public static partial ReadOnlySpan<byte> Vertices();

        [Utf8("PolygonVertexIndex")]
        public static partial ReadOnlySpan<byte> Indices();

        [Utf8("LayerElementNormal")]
        public static partial ReadOnlySpan<byte> NormalInfo();

        [Utf8("Normals")]
        public static partial ReadOnlySpan<byte> Normals();

        [Utf8("LayerElementMaterial")]
        public static partial ReadOnlySpan<byte> MaterialInfo();

        [Utf8("Materials")]
        public static partial ReadOnlySpan<byte> Materials();
    }
}
