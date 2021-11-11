#nullable enable
using Cysharp.Threading.Tasks;
using Elffy.Shapes;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Elffy.Serialization.Wavefront
{
    [Obsolete("Not implemented yet", true)]
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
            var (file, token) = state;
            model.TryGetHostScreen(out var screen);
            Debug.Assert(screen is not null);
            token.ThrowIfCancellationRequested();

            await UniTask.SwitchToThreadPool();
            token.ThrowIfCancellationRequested();

            ObjSemantics<Vertex>? objSemantics = null;
            try {
                using(var stream = file.GetStream())
                using(var obj = ObjParser.Parse(stream)) {
                    objSemantics = LoadMesh(obj);
                }
                await screen.TimingPoints.Update.Next(token);
                load.Invoke(objSemantics.Vertices, objSemantics.Indices);
            }
            finally {
                objSemantics?.Dispose();
            }
        }

        private static ObjSemantics<Vertex> LoadMesh(ObjObject obj)
        {
            throw new NotImplementedException();

            //var allEqual = true;
            //foreach(var (pos, uv, normal) in fList) {
            //    if(!(pos == uv && uv == normal)) {
            //        allEqual = false;
            //        break;
            //    }
            //}

            //if(allEqual) {
            //    var indices_ = new UnsafeRawArray<int>(fList.Length, false);
            //    var vertices_ = UnsafeRawArray<Vertex>.Empty;
            //    try {
            //        var vertexCount = 0;
            //        for(int i = 0; i < fList.Length; i++) {
            //            var index = fList[i].Pos;
            //            indices_[i] = index;
            //            vertexCount = Math.Max(vertexCount, index);
            //        }
            //        vertices_ = new UnsafeRawArray<Vertex>(vertexCount, false);
            //        foreach(var index in indices_.AsSpan()) {
            //            vertices_[index] = new Vertex(positions[index], normals[index], uvs[index]);
            //        }
            //    }
            //    catch {
            //        indices_.Dispose();
            //        vertices_.Dispose();
            //        throw;
            //    }
            //    indices = indices_;
            //    vertices = vertices_;
            //}
            //else {
            //    throw new NotImplementedException();
            //}
        }
    }

    internal sealed class ObjSemantics<TVertex> : IDisposable where TVertex : unmanaged
    {
        public ReadOnlySpan<TVertex> Vertices => throw new NotImplementedException();
        public ReadOnlySpan<int> Indices => throw new NotImplementedException();

        internal ObjSemantics()
        {

        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
