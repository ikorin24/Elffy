using System;
using System.Collections;
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

        protected static void AddContents(object target, params object[] contents)
        {
            if(target == null) { throw new ArgumentNullException(nameof(target)); }
            if(contents == null) { throw new ArgumentNullException(nameof(contents)); }
            if(target is IList list) {
                foreach(var item in contents) {
                    list.Add(item);
                }
            }
            else if(target is IAddContents parent) {
                foreach(var item in contents) {
                    parent.AddContent(item);
                }
            }
            else {
                throw new ArgumentException($"Can not add {nameof(contents)} to ${target}.");
            }
        }

        protected static string FromAltString(string source, string alt) => alt;
        protected static int FromAltString(int source, string alt) => int.Parse(alt);
        protected static uint FromAltString(uint source, string alt) => uint.Parse(alt);
        protected static short FromAltString(short source, string alt) => short.Parse(alt);
        protected static ushort FromAltString(ushort source, string alt) => ushort.Parse(alt);
        protected static long FromAltString(long source, string alt) => long.Parse(alt);
        protected static ulong FromAltString(ulong source, string alt) => ulong.Parse(alt);
        protected static byte FromAltString(byte source, string alt) => byte.Parse(alt);
        protected static char FromAltString(char source, string alt) => char.Parse(alt);
        protected static float FromAltString(float source, string alt) => float.Parse(alt);
        protected static double FromAltString(double source, string alt) => double.Parse(alt);
        protected static decimal FromAltString(decimal source, string alt) => decimal.Parse(alt);
        protected static bool FromAltString(bool source, string alt) => bool.Parse(alt);
        protected static T FromAltString<T>(T source, string alt) where T : Enum => (T)Enum.Parse(typeof(T), alt);
    }

    public interface IAddContents
    {
        void AddContent(object contents);
    }
}
