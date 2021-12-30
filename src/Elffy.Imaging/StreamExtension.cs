#nullable enable

#if !NETCOREAPP3_1
#define CAN_SKIP_LOCALS_INIT
#endif

using Elffy.Effective.Unsafes;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using Elffy.Imaging.Internal;

namespace Elffy.Imaging
{
    internal static class StreamExtension
    {
#if CAN_SKIP_LOCALS_INIT
        [SkipLocalsInit]
#endif
        public static void SafeSkip(this Stream stream, int byteLength)
        {
            if(byteLength < 0) {
                ThrowHelper.ThrowArgOutOfRange(nameof(byteLength));
            }
            if(stream.CanSeek) {
                var pos = stream.Position + byteLength;
                if(pos > stream.Length) {
                    stream.Position = stream.Length;
                    ThrowEOS();
                }
                stream.Position = pos;
            }
            else {
                if(byteLength > 128) {
                    using var buf = new UnsafeRawArray<byte>(byteLength, false);
                    SafeRead(stream, buf.AsSpan());
                }
                else {
                    Span<byte> buf = stackalloc byte[byteLength];
                    SafeRead(stream, buf);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SafeRead(this Stream stream, Span<byte> buffer)
        {
            if(stream.Read(buffer) != buffer.Length) {
                ThrowEOS();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void SafeRead(this Stream stream, void* ptr, int byteLength)
        {
            var buf = new Span<byte>(ptr, byteLength);      // check byteLength >= 0
            if(stream.Read(buf) != byteLength) {
                ThrowEOS();
            }
        }

        private static void ThrowEOS() => throw new EndOfStreamException();
    }
}
