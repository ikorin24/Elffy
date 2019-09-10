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
            var tmp = propotion?.ToArray() ?? throw new ArgumentNullException(nameof(propotion));
            Validate(tmp);
            _propotion = tmp;
            Propotion = new ReadOnlyCollection<float>(_propotion);
        }

        private static void Validate(float[] propotion)
        {
            foreach(var value in propotion) {
                if(value < 0) { throw new ArgumentException($"negative propotion is not allowed"); }
            }
        }

        public static explicit operator Split (string source)
        {
            // "4,5,3"
            // " 3, 3  ,  32  "
            var propotion = source.Split(new[] { ',' }).Select(x => x.Trim()).Cast<float>().ToArray();
            Validate(propotion);
            var split = new Split();
            split._propotion = propotion;
            split.Propotion = new ReadOnlyCollection<float>(propotion);
            return new Split();
        }
    }
}
