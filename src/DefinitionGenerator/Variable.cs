using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace DefinitionGenerator
{
    [DebuggerDisplay("{Accessability} {TypeName} {Name}")]
    public class Variable
    {
        public string Name { get; private set; }
        public string TypeName { get; private set; }
        public string Accessability { get; set; }

        public Variable(string name, string typeName)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            TypeName = typeName ?? throw new ArgumentNullException(nameof(typeName));
        }
    }
}
