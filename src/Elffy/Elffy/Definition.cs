using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elffy
{
    public abstract class Definition
    {
        protected virtual void Initialize() { }

        public virtual void Activate() { }
    }

    public static class DefinitionExtension
    {
        public static string FromAltString(this string source, string alt) => alt;

        public static int FromAltString(this int source, string alt) => int.Parse(alt);

        public static uint FromAltString(this uint source, string alt) => uint.Parse(alt);

        public static short FromAltString(this short source, string alt) => short.Parse(alt);

        public static ushort FromAltString(this ushort source, string alt) => ushort.Parse(alt);

        public static long FromAltString(this long source, string alt) => long.Parse(alt);

        public static ulong FromAltString(this ulong source, string alt) => ulong.Parse(alt);

        public static byte FromAltString(this byte source, string alt) => byte.Parse(alt);

        public static char FromAltString(this char source, string alt) => char.Parse(alt);

        public static float FromAltString(this float source, string alt) => float.Parse(alt);

        public static double FromAltString(this double source, string alt) => double.Parse(alt);

        public static decimal FromAltString(this decimal source, string alt) => decimal.Parse(alt);

        public static bool FromAltString(this bool source, string alt) => bool.Parse(alt);

        public static T FromAltString<T>(this T source, string alt) where T : Enum => (T)Enum.Parse(typeof(T), alt);
    }
}
