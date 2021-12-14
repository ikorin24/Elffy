#nullable enable
using System;
using Elffy.Effective.Unsafes;

namespace Elffy.Serialization.Wavefront
{
    public sealed class ObjObject : IDisposable
    {
        private ObjObjectCore _core;

        public ReadOnlySpan<Vector3> Positions => _core.Positions.AsSpan();
        public ReadOnlySpan<Vector3> Normals => _core.Normals.AsSpan();
        public ReadOnlySpan<Vector2> UVs => _core.UVs.AsSpan();

        public ReadOnlySpan<int> PositionIndices => _core.PositionIndices.AsSpan();
        public ReadOnlySpan<int> NormalIndices => _core.NormalIndices.AsSpan();
        public ReadOnlySpan<int> UVIndices => _core.UVIndices.AsSpan();

        ~ObjObject() => Dispose(false);

        internal ObjObject(ref ObjObjectCore core)
        {
            _core = core;
            core = default;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            _core.Dispose();
        }
    }

    internal readonly struct ObjObjectUnsafe : IDisposable
    {
        private readonly ObjObjectCore _core;

        internal ObjObjectUnsafe(ref ObjObjectCore core)
        {
            _core = core;
            core = default;
        }

        public ReadOnlySpan<Vector3> Positions => _core.Positions.AsSpan();
        public ReadOnlySpan<Vector3> Normals => _core.Normals.AsSpan();
        public ReadOnlySpan<Vector2> UVs => _core.UVs.AsSpan();

        public ReadOnlySpan<int> PositionIndices => _core.PositionIndices.AsSpan();
        public ReadOnlySpan<int> NormalIndices => _core.NormalIndices.AsSpan();
        public ReadOnlySpan<int> UVIndices => _core.UVIndices.AsSpan();

        public void Dispose()
        {
            _core.Dispose();
        }
    }

    internal readonly struct ObjObjectCore : IDisposable
    {
        private readonly UnsafeRawList<Vector3> _positions;
        private readonly UnsafeRawList<Vector3> _normals;
        private readonly UnsafeRawList<Vector2> _uvs;
        private readonly UnsafeRawList<int> _positionIndices;
        private readonly UnsafeRawList<int> _normalIndices;
        private readonly UnsafeRawList<int> _uvIndices;

        public UnsafeRawList<Vector3> Positions => _positions;
        public UnsafeRawList<Vector3> Normals => _normals;
        public UnsafeRawList<Vector2> UVs => _uvs;
        public UnsafeRawList<int> PositionIndices => _positionIndices;
        public UnsafeRawList<int> NormalIndices => _normalIndices;
        public UnsafeRawList<int> UVIndices => _uvIndices;

        public ObjObjectCore()
        {
            _positions = new UnsafeRawList<Vector3>();
            _normals = new UnsafeRawList<Vector3>();
            _uvs = new UnsafeRawList<Vector2>();
            _positionIndices = new UnsafeRawList<int>();
            _normalIndices = new UnsafeRawList<int>();
            _uvIndices = new UnsafeRawList<int>();
        }

        public void Dispose()
        {
            _positions.Dispose();
            _normals.Dispose();
            _uvs.Dispose();
            _positionIndices.Dispose();
            _normalIndices.Dispose();
            _uvIndices.Dispose();
        }
    }
}
