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
    public static class ObjParser
    {
        private const string FormatExceptionMessage = "Invalid format or not supported.";

        public static ObjObject Parse(ReadOnlySpan<byte> data)
        {
            if(TryParsePrivate(data, out var obj) == false) { ThrowFormat(); }
            return new ObjObject(ref obj);
        }

        internal static ObjObjectUnsafe ParseUnsafe(ReadOnlySpan<byte> data)
        {
            if(TryParsePrivate(data, out var obj) == false) { ThrowFormat(); }
            return new ObjObjectUnsafe(ref obj);
        }

        public static ObjObject Parse(Stream stream)
        {
            if(stream is null) { ThrowNullArg(nameof(stream)); }
            using var buf = ReadToBuffer(stream, out var length);
            var text = buf.AsSpan(0, length);
            if(TryParsePrivate(text, out var obj) == false) { ThrowFormat(); }
            return new ObjObject(ref obj);
        }

        internal static ObjObjectUnsafe ParseUnsafe(Stream stream)
        {
            if(stream is null) { ThrowNullArg(nameof(stream)); }
            using var buf = ReadToBuffer(stream, out var length);
            var text = buf.AsSpan(0, length);
            if(TryParsePrivate(text, out var obj) == false) { ThrowFormat(); }
            return new ObjObjectUnsafe(ref obj);
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

        private static bool TryParsePrivate(ReadOnlySpan<byte> text, out ObjObjectCore obj)
        {
            ObjObjectCore core = default;
            try {
                core = new ObjObjectCore();
                var lines = text.Lines();
                foreach(Utf8Reader lineReader in lines) {
                    if(lineReader.MoveIfMatch((byte)'#')) {
                        continue;
                    }
                    else if(lineReader.MoveIfMatch((byte)'v', (byte)' ')) {
                        if(TryParseVector3(lineReader.Current, out var pos) == false) { goto FAILURE; }
                        core.Positions.Add(pos);
                    }
                    else if(lineReader.MoveIfMatch((byte)'v', (byte)'t', (byte)' ')) {
                        if(TryParseVector2(lineReader.Current, out var uv) == false) { goto FAILURE; }
                        core.UVs.Add(uv);
                    }
                    else if(lineReader.MoveIfMatch((byte)'v', (byte)'n', (byte)' ')) {
                        if(TryParseVector3(lineReader.Current, out var normal) == false) { goto FAILURE; }
                        core.Normals.Add(normal);
                    }
                    else if(lineReader.MoveIfMatch((byte)'f', (byte)' ')) {
                        if(TryParseFLine(lineReader.Current, ref core) == false) { goto FAILURE; }
                    }
                    else {
                        continue;
                    }
                }
                obj = core;
                return true;
            FAILURE:
                obj = default;
                return false;
            }
            catch {
                core.Dispose();
                obj = default;
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

        private static bool TryParseFLine(ReadOnlySpan<byte> str, ref ObjObjectCore core)
        {
            // <pos_i>/<uv_i>/<normal_i> <pos_i>/<uv_i>/<normal_i> <pos_i>/<uv_i>/<normal_i> ...

            FaceData v0;
            FaceData v1;
            FaceData v2;

            var splits = str.Split((byte)' ', StringSplitOptions.RemoveEmptyEntries);
            using(var e = splits.GetEnumerator()) {
                int? pos;
                int? uv;
                int? normal;

                if(e.MoveNext() == false) { return false; }
                if(TryParseFaceLine(e.Current, out pos, out uv, out normal) == false || pos.HasValue == false) { return false; }
                v0 = new(pos.Value, uv, normal);

                if(e.MoveNext() == false) { return false; }
                if(TryParseFaceLine(e.Current, out pos, out uv, out normal) == false || pos.HasValue == false) { return false; }
                v1 = new(pos.Value, uv, normal);

                if(e.MoveNext() == false) { return false; }
                if(TryParseFaceLine(e.Current, out pos, out uv, out normal) == false || pos.HasValue == false) { return false; }
                v2 = new(pos.Value, uv, normal);

                if(TryAdd(ref core, v0, v1, v2) == false) { return false; }
                while(e.MoveNext()) {
                    v1 = v2;
                    if(TryParseFaceLine(e.Current, out pos, out uv, out normal) == false || pos.HasValue == false) { return false; }
                    v2 = new(pos.Value, uv, normal);
                    if(TryAdd(ref core, v0, v1, v2) == false) { return false; }
                }
            }
            return true;

            static bool TryAdd(ref ObjObjectCore core, in FaceData v0, in FaceData v1, in FaceData v2)
            {
                core.PositionIndices.Add(v0.Pos - 1);
                core.PositionIndices.Add(v1.Pos - 1);
                core.PositionIndices.Add(v2.Pos - 1);
                if(v0.UV.HasValue) {
                    if(v1.UV.HasValue == false || v2.UV.HasValue == false) { return false; }
                    core.UVIndices.Add(v0.UV.Value - 1);
                    core.UVIndices.Add(v1.UV.Value - 1);
                    core.UVIndices.Add(v2.UV.Value - 1);
                }
                if(v0.Normal.HasValue) {
                    if(v1.Normal.HasValue == false || v2.Normal.HasValue == false) { return false; }
                    core.NormalIndices.Add(v0.Normal.Value - 1);
                    core.NormalIndices.Add(v1.Normal.Value - 1);
                    core.NormalIndices.Add(v2.Normal.Value - 1);
                }
                return true;
            }
        }

        private static bool TryParseFaceLine(ReadOnlySpan<byte> str, out int? pos, out int? uv, out int? normal)
        {
            // <pos_i>/<uv_i>/<normal_i>

            var e = str.Split((byte)'/', StringSplitOptions.None).GetEnumerator();
            if(e.MoveNext() == false) { goto FAILURE; }
            pos = Utf8Parser.TryParse(e.Current, out int p, out _) ? p : null;
            if(e.MoveNext() == false) { goto FAILURE; }
            uv = Utf8Parser.TryParse(e.Current, out int t, out _) ? t : null;
            if(e.MoveNext() == false) { goto FAILURE; }
            normal = Utf8Parser.TryParse(e.Current, out int n, out _) ? n : null;
            return true;

        FAILURE:
            pos = null;
            uv = null;
            normal = null;
            return false;
        }

        [DoesNotReturn]
        private static void ThrowNullArg(string message) => throw new ArgumentNullException(message);

        [DoesNotReturn]
        private static void ThrowFormat() => throw new FormatException(FormatExceptionMessage);

        private record struct FaceData(int Pos, int? UV, int? Normal);
    }
}
