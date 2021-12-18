#nullable enable
using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using Elffy.Effective;
using Elffy.Effective.Unsafes;

namespace Elffy
{
    public static class MeshOperations
    {
        public static Span<Vector3> RecalculateNormal<TBufferWriter>(ReadOnlySpan<Vector3> positions, ReadOnlySpan<int> indices, TBufferWriter normalsBuffer) where TBufferWriter : IBufferWriter<Vector3>
        {
            var normals = normalsBuffer.GetSpan(positions.Length);
            RecalculateNormal(positions, indices, normals);
            normalsBuffer.Advance(normals.Length);
            return normals;
        }

        public static void RecalculateNormal(ReadOnlySpan<Vector3> positions, ReadOnlySpan<int> indices, Span<Vector3> normals)
        {
            if(indices.Length % 3 != 0) {
                ThrowArgumentIndicesLengthInvalid();
            }

            // [NOTE]
            // Sharp edge is not supported.

            normals.Clear();

            using var countsBuf = new ValueTypeRentMemory<int>(positions.Length, true);
            var counts = countsBuf.AsSpan();

            var faces = indices.MarshalCast<int, Face>();
            foreach(var f in faces) {
                var n = Vector3.Cross(positions[f.I1] - positions[f.I0], positions[f.I2] - positions[f.I0]).Normalized();
                normals[f.I0] += n;
                normals[f.I1] += n;
                normals[f.I2] += n;
                counts[f.I0] += 1;
                counts[f.I1] += 1;
                counts[f.I2] += 1;
            }
            for(int i = 0; i < positions.Length; i++) {
                normals[i] /= counts[i];
            }
        }

        public static void RecalculateNormal(Span<Vertex> vertices, ReadOnlySpan<int> indices)  // TODO: something wrong
        {
            if(indices.Length % 3 != 0) {
                ThrowArgumentIndicesLengthInvalid();
            }

            for(int i = 0; i < vertices.Length; i++) {
                vertices[i].Normal = default;
            }

            using var counts = new ValueTypeRentMemory<int>(vertices.Length, true);
            var faces = indices.MarshalCast<int, Face>();
            foreach(var f in faces) {
                var n = Vector3.Cross(vertices[f.I1].Position - vertices[f.I0].Position, vertices[f.I2].Position - vertices[f.I0].Position).Normalized();
                vertices[f.I0].Normal += n;
                vertices[f.I1].Normal += n;
                vertices[f.I2].Normal += n;
                counts[f.I0] += 1;
                counts[f.I1] += 1;
                counts[f.I2] += 1;
            }

            for(int i = 0; i < vertices.Length; i++) {
                vertices[i].Normal /= counts[i];
            }
        }

        public static (Vertex[] vertices, int[] indices) CreateInterleavedVertices(
            ReadOnlySpan<Vector3> positions, ReadOnlySpan<int> positionIndices,
            ReadOnlySpan<Vector3> normals, ReadOnlySpan<int> normalIndices,
            ReadOnlySpan<Vector2> uvs, ReadOnlySpan<int> uvIndices)
        {
            using var verticesBuffer = new UnsafeBufferWriter<Vertex>();
            using var indicesBuffer = new UnsafeBufferWriter<int>();
            var (vertices, indices) = CreateInterleavedVertices(positions, positionIndices, normals, normalIndices, uvs, uvIndices, verticesBuffer, indicesBuffer);
            return (vertices.ToArray(), indices.ToArray());
        }

        public static InterleavedMesh<Vertex> CreateInterleavedVertices(
            ReadOnlySpan<Vector3> positions, ReadOnlySpan<int> positionIndices,
            ReadOnlySpan<Vector3> normals, ReadOnlySpan<int> normalIndices,
            ReadOnlySpan<Vector2> uvs, ReadOnlySpan<int> uvIndices,
            IBufferWriter<Vertex> verticesWriter, IBufferWriter<int> indicesWriter)
        {
            var isValid = (positionIndices.Length == normalIndices.Length) && (positionIndices.Length == uvIndices.Length);
            if(isValid == false) {
                throw new ArgumentException();
            }

            var len = positionIndices.Length;
            using var dic = new BufferPooledDictionary<Key_PNT, int>(len);

            var vertices = verticesWriter.GetSpan(len);
            var indices = indicesWriter.GetSpan(len);
            var verticesCount = 0;
            var indicesCount = 0;

            for(int i = 0; i < len; i++) {
                var key = new Key_PNT(positionIndices.At(i), normalIndices.At(i), uvIndices.At(i));
                if(dic.TryGetValue(key, out var index)) {
                    indices[indicesCount++] = index;
                }
                else {
                    index = dic.Count;
                    indices[indicesCount++] = index;
                    dic.Add(key, index);
                    vertices[verticesCount++] = new Vertex(positions[key.P], normals[key.N], uvs[key.T]);
                }
            }

            verticesWriter.Advance(verticesCount);
            indicesWriter.Advance(indicesCount);

            return new(vertices.Slice(0, verticesCount), indices.Slice(0, indicesCount));
        }

        private record struct Face(int I0, int I1, int I2);
        private record struct Key_PNT(int P, int N, int T);

        [DoesNotReturn]
        private static void ThrowArgumentIndicesLengthInvalid() => throw new ArgumentException();
    }

    public readonly ref struct InterleavedMesh<TVertex> where TVertex : unmanaged
    {
        public readonly Span<TVertex> Vertices;
        public readonly Span<int> Indices;

        public InterleavedMesh(Span<TVertex> vertices, Span<int> indices)
        {
            Vertices = vertices;
            Indices = indices;
        }

        public void Deconstruct(out Span<TVertex> vertices, out Span<int> indices)
        {
            vertices = Vertices;
            indices = Indices;
        }
    }
}
