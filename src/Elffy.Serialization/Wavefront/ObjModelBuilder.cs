#nullable enable
using Cysharp.Threading.Tasks;
using Elffy.Effective.Unsafes;
using Elffy.Shapes;
using System.Diagnostics;
using System.Threading;

namespace Elffy.Serialization.Wavefront
{
    public static class ObjModelBuilder
    {
        private sealed record StateObject(ResourceFile File, CancellationToken CancellationToken);

        private static readonly Model3DBuilderDelegate<StateObject> _build = Build;

        public static Model3D CreateLazyLoadingObj(ResourceFile file, CancellationToken cancellationToken = default)
        {
            ResourceFile.ThrowArgumentExceptionIfInvalid(file);
            var obj = new StateObject(file, cancellationToken);
            return Model3D.Create(obj, _build);
        }

        private static async UniTask Build(StateObject state, Model3D model, Model3DLoadMeshDelegate load)
        {
            var (file, ct) = state;
            model.TryGetHostScreen(out var screen);
            Debug.Assert(screen is not null);
            ct.ThrowIfCancellationRequested();

            await UniTask.SwitchToThreadPool();
            ct.ThrowIfCancellationRequested();

            using var verticesBuffer = new UnsafeBufferWriter<Vertex>();
            using var indicesBuffer = new UnsafeBufferWriter<int>();
            ParseFromFile(file, verticesBuffer, indicesBuffer);

            await screen.TimingPoints.Update.Next(ct);
            load.Invoke(verticesBuffer.WrittenSpan, indicesBuffer.WrittenSpan);
        }

        private static void ParseFromFile(ResourceFile file, UnsafeBufferWriter<Vertex> verticesBuffer, UnsafeBufferWriter<int> indicesBuffer)
        {
            using var stream = file.GetStream();
            using var obj = ObjParser.ParseUnsafe(stream);
            var hasNormal = !obj.NormalIndices.IsEmpty;
            BuildMesh(obj, hasNormal, verticesBuffer, indicesBuffer);
            if(hasNormal == false) {
                MeshOperations.RecalculateNormal(verticesBuffer.WrittenSpan, indicesBuffer.WrittenSpan);
            }
        }

        private static void BuildMesh(in ObjObjectUnsafe obj, bool hasNormal, UnsafeBufferWriter<Vertex> verticesBuffer, UnsafeBufferWriter<int> indicesBuffer)
        {
            using var normalIndicesBuf = hasNormal ? UnsafeRawArray<int>.Empty : new UnsafeRawArray<int>(obj.PositionIndices.Length, true);
            var normals = hasNormal ? obj.Normals : stackalloc Vector3[1] { Vector3.UnitX };
            var normalIndices = hasNormal ? obj.NormalIndices : normalIndicesBuf.AsSpan();

            var hasUV = !obj.UVIndices.IsEmpty;
            using var uvIndicesBuf = hasUV ? UnsafeRawArray<int>.Empty : new UnsafeRawArray<int>(obj.PositionIndices.Length, true);
            var uvs = hasUV ? obj.UVs : stackalloc Vector2[1] { Vector2.Zero };
            var uvIndices = hasUV ? obj.UVIndices : uvIndicesBuf.AsSpan();
            MeshOperations.CreateInterleavedVertices(obj.Positions, obj.PositionIndices,
                                                     normals, normalIndices,
                                                     uvs, uvIndices, verticesBuffer, indicesBuffer);
        }
    }
}
