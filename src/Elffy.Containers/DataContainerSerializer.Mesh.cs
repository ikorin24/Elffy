#nullable enable
using Microsoft.Win32.SafeHandles;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Elffy
{
    unsafe partial class DataContainerSerializer
    {
        public static void ReadMeshContent(in ContainerHeader header, UniquePtr content, ulong contentByteLength, out MeshContent mesh, out UniquePtr meshData)
        {
            try {
                CheckPlatformEndian();
                if(header.Type != ContainerType.Mesh) { throw new ArgumentException("The content is not mesh."); }

                if(contentByteLength != header.ContentSize) {
                    throw new FormatException("Content size is not match with the header.");
                }

                using var decompressed = Decompress(header, content.Move(), contentByteLength, out var decompressedByteLength);
                var reader = new PointerStreamingReader(decompressed.GetPtr<byte>());
                //MeshContent mesh;
                mesh.VertexSize = reader.Read<uint>();
                mesh.IndexSize = reader.Read<uint>();
                mesh.VertexFieldCount = reader.Read<uint>();
                mesh.VertexFields = reader.ReadArray<VertexFieldInfo>(mesh.VertexFieldCount);

                // TODO: CheckVertexType

                var vertexCount = reader.Read<ulong>();
                mesh.VerticesByteLength = checked(vertexCount * mesh.VertexSize);
                mesh.Vertices = reader.ReadArray<byte>(mesh.VerticesByteLength);

                var indexCount = reader.Read<ulong>();
                mesh.IndicesByteLength = checked(indexCount * sizeof(int));
                mesh.Indices = reader.ReadArray<byte>(mesh.IndicesByteLength);

                meshData = decompressed.Move();
            }
            finally {
                content.Dispose();
            }
        }

        public static void WriteMesh(SafeFileHandle handle, Type vertexType, void* vertices, ulong verticesByteLength, uint* indices, ulong indicesByteLength)
        {
            const uint IndexSize = sizeof(uint);

            ArgumentNullException.ThrowIfNull(handle);
            ArgumentNullException.ThrowIfNull(vertexType);
            var vertexTypeData = VertexTypeData.GetVertexTypeData(vertexType);
            uint vertexSize = (uint)vertexTypeData.VertexSize;
            uint fieldCount = (uint)vertexTypeData.FieldCount;

            ulong contentSize = (ulong)sizeof(uint) +                           // vertexSize
                                (ulong)sizeof(uint) +                           // indexSize
                                (ulong)sizeof(uint) +                           // fieldCount
                                (ulong)sizeof(VertexFieldInfo) * fieldCount +   // fields
                                sizeof(ulong) +                                 // vertexCount
                                verticesByteLength +                            // vertices
                                sizeof(ulong) +                                 // indexCount
                                indicesByteLength;                              // indices
            var size = (ulong)sizeof(ContainerHeader) + contentSize;

            if(size > int.MaxValue) {
                throw new NotSupportedException("Large content is not supported");
            }

            byte* buf = null;
            try {
                buf = (byte*)NativeMemory.Alloc((nuint)size);
                var writer = new PointerStreamingWriter(buf);
                writer.Write(new ContainerHeader
                {
                    MagicWord = ContainerHeader.ValidMagicWord,
                    FormatVersion = CurrentFormatVersion,
                    Type = ContainerType.Mesh,
                    CompressionType = ContainerCompressionType.None,
                    ContentSize = contentSize,
                    ContentDecompressedSize = contentSize,
                });;
                writer.Write(vertexSize);
                writer.Write(IndexSize);
                writer.Write(fieldCount);
                foreach(var fieldData in vertexTypeData.GetFields()) {
                    writer.Write(new VertexFieldInfo
                    {
                        Kind = fieldData.Semantics,
                        Offset = (uint)fieldData.ByteOffset,
                        MarshalType = fieldData.MarshalType,
                        MarshalCount = (uint)fieldData.MarshalCount,
                    });
                }

                var vertexCount = verticesByteLength / vertexSize;
                writer.Write(vertexCount);
                writer.Write(vertices, verticesByteLength);
                var indexCount = indicesByteLength / IndexSize;
                writer.Write((ulong)indexCount);
                writer.Write(indices, indicesByteLength);

                Debug.Assert(writer.Offset == size);

                RandomAccess.Write(handle, new ReadOnlySpan<byte>(buf, (int)size), 0);
            }
            finally {
                NativeMemory.Free(buf);
            }
        }
    }
}
