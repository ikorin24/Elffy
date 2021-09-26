#nullable enable
using System;
using Elffy.Effective.Unsafes;

namespace Elffy.Serialization.Wavefront
{
    internal sealed class ObjObject : IDisposable
    {
        private UnsafeRawList<Vector3> _positions;
        private UnsafeRawList<Vector3> _normals;
        private UnsafeRawList<Vector2> _uvs;
        private UnsafeRawList<ObjFace> _faces;

        public ReadOnlySpan<Vector3> Positions => _positions.AsSpan();
        public ReadOnlySpan<Vector3> Normals => _normals.AsSpan();
        public ReadOnlySpan<Vector2> UVs => _uvs.AsSpan();
        public ReadOnlySpan<ObjFace> Faces => _faces.AsSpan();

        ~ObjObject() => Dispose(false);

        internal ObjObject(UnsafeRawList<Vector3> positions, UnsafeRawList<Vector3> normals, UnsafeRawList<Vector2> uvs, UnsafeRawList<ObjFace> faces)
        {
            _positions = positions;
            _normals = normals;
            _uvs = uvs;
            _faces = faces;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            _positions.Dispose();
            _normals.Dispose();
            _uvs.Dispose();
            _faces.Dispose();
        }
    }

    internal readonly struct ObjFace
    {
        public readonly int Position;
        public readonly int UV;
        public readonly int Normal;
    }
}
