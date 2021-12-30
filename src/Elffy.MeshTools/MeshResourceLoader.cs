#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Elffy
{
    public static class MeshResourceLoader
    {
        public unsafe static Mesh LoadMeshContainer(ResourceFile file)
        {
            ContainerHeader header;
            nuint contentBufLen;
            using var contentBuf = UniquePtr.Null();
            using(var handle = file.GetHandle()) {
                LoadContainerHeader(handle, out header);
                if(header.Type != ContainerType.Mesh) {
                    throw new ArgumentException($"{nameof(file)} is not mesh container.");
                }
                contentBufLen = checked((nuint)header.ContentSize);
                contentBuf.ResetMalloc(contentBufLen);
                handle.Read(contentBuf.Ptr, contentBufLen, sizeof(ContainerHeader));
            }
            DataContainerSerializer.ReadMeshContent(header, contentBuf.Move(), contentBufLen, out var mesh, out var meshData);
            try {
                return new Mesh(mesh, ref meshData);
            }
            finally {
                meshData.Dispose();
            }
        }

        private unsafe static void LoadContainerHeader(ResourceFileHandle handle, out ContainerHeader header)
        {
            if(BitConverter.IsLittleEndian == false) {
                ThrowNotSupportedPlatform();
                static void ThrowNotSupportedPlatform() => throw new PlatformNotSupportedException("Big endian platform is not supported.");
            }
            Unsafe.SkipInit(out header);
            var buf = MemoryMarshal.CreateSpan(ref Unsafe.As<ContainerHeader, byte>(ref header), sizeof(ContainerHeader));
            if(handle.Read(buf, 0) != buf.Length) {
                ThrowInvalidFile();
                static void ThrowInvalidFile() => throw new ArgumentException("The file is too short to read container header.");
            }
            DataContainerSerializer.ReadHeader(buf);
        }
    }
}
