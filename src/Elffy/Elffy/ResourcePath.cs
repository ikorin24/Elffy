#nullable enable
using System;
using Elffy.Effective.Unsafes;

namespace Elffy
{
    public static class ResourcePath
    {
        private const char Splitter = '/';
        private const char Dot = '.';

        public static ReadOnlySpan<char> GetDirectoryName(string name)
        {
            return name is null ? ReadOnlySpan<char>.Empty : GetDirectoryName(name.AsSpan());
        }

        public static ReadOnlySpan<char> GetDirectoryName(ReadOnlySpan<char> name)
        {
            for(int i = name!.Length - 1; i >= 0; i--) {
                if(name.At(i) == Splitter) {
                    return name.SliceUnsafe(0, i);
                }
            }
            return ReadOnlySpan<char>.Empty;
        }

        public static ReadOnlySpan<char> GetFileName(string name)
        {
            return name is null ? ReadOnlySpan<char>.Empty : GetFileName(name.AsSpan());
        }

        public static ReadOnlySpan<char> GetFileName(ReadOnlySpan<char> name)
        {
            for(int i = name!.Length - 1; i >= 0; i--) {
                if(name.At(i) == Splitter) {
                    if(i == name.Length - 1) {
                        return ReadOnlySpan<char>.Empty;
                    }
                    else {
                        return name.SliceUnsafe(i + 1);
                    }
                }
            }
            return name;
        }

        public static ReadOnlySpan<char> GetFileNameWithoutExtension(string name)
        {
            return name is null ? ReadOnlySpan<char>.Empty : GetFileNameWithoutExtension(name.AsSpan());
        }

        public static ReadOnlySpan<char> GetFileNameWithoutExtension(ReadOnlySpan<char> name)
        {
            var filename = GetFileName(name);
            for(int i = filename.Length - 1; i >= 0; i--) {
                if(filename.At(i) == Dot) {
                    return filename.SliceUnsafe(0, i);
                }
            }
            return filename;
        }

        public static ReadOnlySpan<char> GetExtension(string name)
        {
            return name is null ? ReadOnlySpan<char>.Empty : GetExtension(name.AsSpan());
        }

        public static ReadOnlySpan<char> GetExtension(ReadOnlySpan<char> name)
        {
            for(int i = name.Length - 1; i >= 0; i--) {
                if(name.At(i) == Dot) {
                    return name.SliceUnsafe(i);
                }
            }
            return ReadOnlySpan<char>.Empty;
        }
    }
}
