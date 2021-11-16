#nullable enable
using Elffy.Effective.Unsafes;
using System;
using System.IO;

namespace Elffy.Imaging.Internal
{
    internal static class StreamExtension
    {
        public static UnsafeRawList<byte> ReadToEnd(this Stream stream, out int length)
        {
            if(stream is null) {
                throw new ArgumentNullException(nameof(stream));
            }
            var buf = new UnsafeRawList<byte>(0);
            try {
                length = 0;
                while(true) {
                    var span = buf.Extend(1024);
                    var readLen = stream.Read(span);
                    length += readLen;
                    if(readLen == 0) { break; }
                }
                return buf;
            }
            catch {
                buf.Dispose();
                throw;
            }
        }
    }
}
