using Elffy.Exceptions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elffy.UI
{
    public partial class Split
    {
        private float[] _propotion;
        public ReadOnlyCollection<float> Propotion { get; private set; }

        private Split()
        {
        }

        public Split(ICollection<float> propotion)
        {
            ArgumentChecker.ThrowIfNullArg(propotion, nameof(propotion));
            var tmp = propotion.ToArray();
            ArgumentChecker.ThrowArgumentIf(tmp.Any(value => value < 0), "negative propotion is not allowed");
            _propotion = tmp;
            Propotion = new ReadOnlyCollection<float>(_propotion);
        }

        public static explicit operator Split (string source)
        {
            // "4,5,3"
            // " 3, 3  ,  32  "
            var propotion = source.Split(new[] { ',' }).Select(x => x.Trim()).Cast<float>().ToArray();
            ArgumentChecker.ThrowArgumentIf(propotion.Any(value => value < 0), "negative propotion is not allowed");
            var split = new Split();
            split._propotion = propotion;
            split.Propotion = new ReadOnlyCollection<float>(propotion);
            return new Split();
        }
    }
}
