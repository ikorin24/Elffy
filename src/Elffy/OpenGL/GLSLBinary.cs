#nullable enable
using Elffy.Effective;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Elffy.OpenGL
{
    internal sealed class GLSLBinary
    {
        private ValueTypeRentMemory<int> _formats;
        private ValueTypeRentMemory<byte> _binary;

        private GLSLBinary(ValueTypeRentMemory<int> formats, ValueTypeRentMemory<byte> binary)
        {
            _formats = formats;
            _binary = binary;
        }

        public static Task<GLSLBinary> LoadAsync(Stream stream, bool closeStream = true)
        {
            if(stream is null) { throw new ArgumentNullException(nameof(stream)); }

            return Task.Factory.StartNew(state =>
            {
                Debug.Assert(state is Stream);
                var stream = Unsafe.As<Stream>(state);

                using(var reader = new BinaryReader(stream, Encoding.UTF8, !closeStream)) {
                    var formatsLen = reader.ReadInt32();
                    if(formatsLen % sizeof(int) != 0) {
                        throw new InvalidDataException();
                    }
                    var formats = new ValueTypeRentMemory<int>(formatsLen / 4);
                    reader.Read(formats.Span.MarshalCast<int, byte>());
                    var binary = new ValueTypeRentMemory<byte>(reader.ReadInt32());
                    reader.Read(binary.Span);
                    return new GLSLBinary(formats, binary);
                }
            }, stream);
        }

        public Task DumpAsync(Stream stream, bool closeStream = true)
        {
            if(stream is null) { throw new ArgumentNullException(nameof(stream)); }
            if(!stream.CanWrite) { throw new ArgumentException(); }
            return Task.Factory.StartNew(state =>
            {
                Debug.Assert(state is Stream);
                var stream = Unsafe.As<Stream>(state);
                var f = _formats.Span.MarshalCast<int, byte>();
                var bin = _binary.Span;

                using(var writer = new BinaryWriter(stream, Encoding.UTF8, !closeStream)) {
                    writer.Write(f.Length);
                    writer.Write(f);
                    writer.Write(bin.Length);
                    writer.Write(bin);
                }
            }, stream);
        }
    }
}
