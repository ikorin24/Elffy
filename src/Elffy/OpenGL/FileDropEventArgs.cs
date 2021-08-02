#nullable enable
using System;
using System.Text;

namespace Elffy.OpenGL
{
    public unsafe readonly ref struct FileDropEventArgs
    {
        private readonly byte** _files;
        public readonly int FileCount;

        internal FileDropEventArgs(int count, byte** files)
        {
            _files = files;
            FileCount = count;
        }

        public string GetFileNameString(int index)
        {
            return Encoding.UTF8.GetString(GetFileName(index));
        }

        public ReadOnlySpan<byte> GetFileName(int index)
        {
            if((uint)index >= (uint)FileCount) {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            var file = _files[index];
            if(file == null) {
                return ReadOnlySpan<byte>.Empty;
            }
            else {
                var j = 0;
                for(j = 0; file[j] != 0; j++) { }
                return new ReadOnlySpan<byte>(file, j);
            }
        }
    }

    internal delegate void FileDropEventHandler(WindowGLFW window, FileDropEventArgs e);
}
