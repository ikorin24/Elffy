using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elffy
{
    public abstract class Definition
    {
        public const string ValueConverterMethodName = nameof(FromAltString);

        protected virtual void Initialize() { }

        public virtual void Activate() { }

        protected static void FromAltString(string alt, Action<string> setter)  => setter(alt);
        protected static void FromAltString(string alt, Action<int> setter)     => setter(int.Parse(alt));
        protected static void FromAltString(string alt, Action<uint> setter)    => setter(uint.Parse(alt));
        protected static void FromAltString(string alt, Action<short> setter)   => setter(short.Parse(alt));
        protected static void FromAltString(string alt, Action<ushort> setter)  => setter(ushort.Parse(alt));
        protected static void FromAltString(string alt, Action<long> setter)    => setter(long.Parse(alt));
        protected static void FromAltString(string alt, Action<ulong> setter)   => setter(ulong.Parse(alt));
        protected static void FromAltString(string alt, Action<byte> setter)    => setter(byte.Parse(alt));
        protected static void FromAltString(string alt, Action<char> setter)    => setter(char.Parse(alt));
        protected static void FromAltString(string alt, Action<float> setter)   => setter(float.Parse(alt));
        protected static void FromAltString(string alt, Action<double> setter)  => setter(double.Parse(alt));
        protected static void FromAltString(string alt, Action<decimal> setter) => setter(decimal.Parse(alt));
        protected static void FromAltString(string alt, Action<bool> setter)    => setter(bool.Parse(alt));
        protected static void FromAltString<T>(string alt, Action<T> setter) where T : Enum => setter((T)Enum.Parse(typeof(T), alt));
    }
}
