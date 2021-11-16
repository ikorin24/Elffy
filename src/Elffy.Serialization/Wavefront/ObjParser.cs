#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Buffers.Text;
using Elffy.Effective.Unsafes;
using Elffy.Serialization.Text;
using Elffy.Text;

namespace Elffy.Serialization.Wavefront
{
    internal static class ObjParser
    {
        public static ObjObject Parse(Stream stream)
        {
            if(stream is null) { ThrowNullArg(nameof(stream)); }
            using var buf = ReadToBuffer(stream, out var length);
            var text = buf.AsSpan(0, length);
            if(TryParsePrivate(text, out var obj) == false) {
                throw new FormatException();
            }
            return obj;
        }

        private static UnsafeRawList<byte> ReadToBuffer(Stream stream, out int length)
        {
            if(stream.CanSeek) {
                var streamLen = stream.Length;
                if(streamLen <= int.MaxValue) {
                    var buffer = new UnsafeRawList<byte>((int)streamLen);
                    try {
                        length = stream.Read(buffer.Extend((int)streamLen, false));
                        return buffer;
                    }
                    catch {
                        buffer.Dispose();
                        throw;
                    }
                }
                else {
                    throw new NotSupportedException();
                }
            }
            else {
                var buffer = new UnsafeRawList<byte>();
                var totalLen = 0;
                while(true) {
                    const int BlockSize = 1024;
                    var readLen = stream.Read(buffer.Extend(BlockSize, false));
                    totalLen += readLen;
                    if(readLen < BlockSize) { break; }
                }
                length = totalLen;
                return buffer;
            }
        }

        private static bool TryParsePrivate(ReadOnlySpan<byte> text, [MaybeNullWhen(false)] out ObjObject obj)
        {
            var positions = new UnsafeRawList<Vector3>();
            var normals = new UnsafeRawList<Vector3>();
            var uvs = new UnsafeRawList<Vector2>();
            var fList = new UnsafeRawList<ObjFace>();
            try {
                var lines = text.Lines();
                foreach(var line in lines) {
                    var lineReader = new Utf8Reader(line);
                    if(lineReader.MoveIfMatch((byte)'#')) {
                        continue;
                    }
                    else if(lineReader.MoveIfMatch((byte)'v', (byte)' ')) {
                        if(TryParseVector3(lineReader.Current, out var pos) == false) { goto FAILURE; }
                        positions.Add(pos);
                    }
                    else if(lineReader.MoveIfMatch((byte)'v', (byte)'t', (byte)' ')) {
                        if(TryParseVector2(lineReader.Current, out var uv) == false) { goto FAILURE; }
                        uvs.Add(uv);
                    }
                    else if(lineReader.MoveIfMatch((byte)'v', (byte)'n', (byte)' ')) {
                        if(TryParseVector3(lineReader.Current, out var normal) == false) { goto FAILURE; }
                        normals.Add(normal);
                    }
                    else if(lineReader.MoveIfMatch((byte)'f', (byte)' ')) {
                        if(TryParseFLine(lineReader.Current) == false) { goto FAILURE; }
                    }
                    else {
                        continue;
                    }
                }
                obj = new ObjObject(positions, normals, uvs, fList);
                return true;
            FAILURE:
                positions.Dispose();
                normals.Dispose();
                uvs.Dispose();
                fList.Dispose();
                obj = null;
                return false;
            }
            catch {
                positions.Dispose();
                normals.Dispose();
                uvs.Dispose();
                fList.Dispose();
                obj = null;
                return false;
            }
        }

        private static bool TryParseVector3(ReadOnlySpan<byte> str, out Vector3 vec)
        {
            // <x> <y> <z>
            var e = str.Split((byte)' ', StringSplitOptions.RemoveEmptyEntries).GetEnumerator();
            if(e.MoveNext() == false || Utf8Parser.TryParse(e.Current, out vec.X, out _) == false ||
               e.MoveNext() == false || Utf8Parser.TryParse(e.Current, out vec.Y, out _) == false ||
               e.MoveNext() == false || Utf8Parser.TryParse(e.Current, out vec.Z, out _) == false) {
                vec = default;
                return false;
            }
            return true;
        }

        private static bool TryParseVector2(ReadOnlySpan<byte> str, out Vector2 vec)
        {
            // <x> <y>
            var e = str.Split((byte)' ', StringSplitOptions.RemoveEmptyEntries).GetEnumerator();
            if(e.MoveNext() == false || Utf8Parser.TryParse(e.Current, out vec.X, out _) == false ||
               e.MoveNext() == false || Utf8Parser.TryParse(e.Current, out vec.Y, out _) == false) {
                vec = default;
                return false;
            }
            return true;
        }

        private static bool TryParseFLine(ReadOnlySpan<byte> str)
        {
            // <pos_i>/<uv_i>/<normal_i> <pos_i>/<uv_i>/<normal_i> <pos_i>/<uv_i>/<normal_i> ...

            // TODO: more than 4
            Span<(int Pos, int UV, int Normal)> buf = stackalloc (int Pos, int UV, int Normal)[4];
            var vCount = 0;
            var splits = str.Split((byte)' ', StringSplitOptions.RemoveEmptyEntries);
            foreach(var v in splits) {
                if(TryParseFLineVertexData(v, out var pos, out var uv, out var normal) == false) {
                    return false;
                }
                buf[vCount] = (Pos: pos, UV: uv, Normal: normal);
                vCount++;
            }

            throw new NotImplementedException();    // TODO: Triangulation
        }

        private static bool TryParseFLineVertexData(ReadOnlySpan<byte> str, out int pos, out int uv, out int normal)
        {
            // <pos_i>/<uv_i>/<normal_i>

            var e = str.Split((byte)'/').GetEnumerator();
            if(e.MoveNext() == false || Utf8Parser.TryParse(e.Current, out pos, out _) == false ||
               e.MoveNext() == false || Utf8Parser.TryParse(e.Current, out uv, out _) == false ||
               e.MoveNext() == false || Utf8Parser.TryParse(e.Current, out normal, out _) == false) {
                pos = default;
                uv = default;
                normal = default;
                return false;
            }
            return true;
        }

        [DoesNotReturn]
        private static void ThrowNullArg(string message) => throw new ArgumentNullException(message);
    }
}
