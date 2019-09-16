using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DefinitionGenerator
{
    public class Using
    {
        public string Namespace { get; private set; }

        public Using(string crlNamespace)
        {
            Namespace = crlNamespace ?? throw new ArgumentNullException(nameof(crlNamespace));
        }
    }
}
