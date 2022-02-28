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
            var screen = model.GetValidScreen();
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

            var hasUV = !obj.UVIndices.IsEmpty;
            using var uvIndicesBuf = hasUV ? UnsafeRawArray<int>.Empty : new UnsafeRawArray<int>(obj.PositionIndices.Length, true);
            var uvs = hasUV ? obj.UVs : stackalloc Vector2[1] { Vector2.Zero };
            var uvIndices = hasUV ? obj.UVIndices : uvIndicesBuf.AsSpan();

            var hasNormal = !obj.NormalIndices.IsEmpty;
            if(hasNormal == false) {
                using var normalsBuf = new UnsafeBufferWriter<Vector3>();
                var positions = obj.Positions;
                var posNormalIndices = obj.PositionIndices;
                var normals = MeshOperations.RecalculateNormal(positions, posNormalIndices, normalsBuf);
                MeshOperations.CreateInterleavedVertices(positions, posNormalIndices, normals, posNormalIndices, uvs, uvIndices, verticesBuffer, indicesBuffer);
            }
            else {
                MeshOperations.CreateInterleavedVertices(obj.Positions, obj.PositionIndices, obj.Normals, obj.NormalIndices, uvs, uvIndices, verticesBuffer, indicesBuffer);
            }
        }
    }
}
