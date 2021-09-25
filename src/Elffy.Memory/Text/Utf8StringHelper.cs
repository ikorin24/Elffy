#nullable enable
using System;
using System.Text;

namespace Elffy.Text
{
    public static class Utf8StringHelper
    {
        private static readonly Encoding _utf8 = Encoding.UTF8;

        public static string ToString(ReadOnlySpan<byte> str)
        {
            return _utf8.GetString(str);
        }
    }
}
