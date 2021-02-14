#nullable enable

#if !NETCOREAPP3_1
#define CAN_SKIP_LOCALS_INIT
#endif

using Elffy;
using Elffy.Effective;
using Elffy.Effective.Unsafes;
using Elffy.Imaging.Internal;
using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.IO;
using System.Diagnostics;

namespace Elffy.Imaging
{
    public unsafe static class JpegParser
    {
        private static ReadOnlySpan<byte> APP0 => new byte[2] { 0xff, 0xe0, };
        private static ReadOnlySpan<byte> SOI => new byte[2] { 0xff, 0xd8 };
        private static ReadOnlySpan<byte> EOI => new byte[2] { 0xff, 0xd9 };
        private static ReadOnlySpan<byte> DQT => new byte[2] { 0xff, 0xdb };
        private static ReadOnlySpan<byte> DHT => new byte[2] { 0xff, 0xd4 };
        private static ReadOnlySpan<byte> SOS => new byte[2] { 0xff, 0xda };

#if CAN_SKIP_LOCALS_INIT
        [SkipLocalsInit]
#endif
        public static Image Parse(Stream stream)
        {
            Span<byte> marker = stackalloc byte[2];
            stream.SafeRead(marker);
            if(marker.SequenceEqual(SOI) == false) {
                ThrowHelper.ThrowFormatException("This is not jpeg.");
            }

            Span<byte> buf = stackalloc byte[2];
            while(true) {
                stream.SafeRead(marker);    // read marker
                Debug.WriteLine(marker[0].ToString("x") + marker[1].ToString("x"));
                                
                if(marker.SequenceEqual(EOI)) {
                    break;
                }
                stream.SafeRead(buf);
                var segSize = BinaryPrimitives.ReadUInt16BigEndian(buf) - 2;

                if(marker.SequenceEqual(SOS)) {
                    ParseSOS(stream, segSize);
                }
                else {
                    // Skip the segment
                    stream.SafeSkip(segSize);
                }
            }

            throw new NotImplementedException();
        }

#if CAN_SKIP_LOCALS_INIT
        [SkipLocalsInit]
#endif
        private static void ParseSOS(Stream stream, int segmentSize)
        {
            Span<byte> buf = stackalloc byte[segmentSize];
            stream.SafeRead(buf);
            var n = buf[0];

            // ECS (Entropy-coded segment)
        }
    }
}
