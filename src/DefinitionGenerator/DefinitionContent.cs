using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DefinitionGenerator
{
    public class DefinitionContent
    {
        public ReadOnlyCollection<Using> Usings { get; private set; }
        public ReadOnlyCollection<Variable> Variables { get; private set; }
        public ReadOnlyCollection<string> PropertySetStrings { get; private set; }
        public ReadOnlyCollection<Dependency> Dependencies { get; private set; }

        public DefinitionContent(IList<Using> usings, IList<Variable> variables, IList<string> propertySetStrings, IList<Dependency> dependencies)
        {
            Usings = new ReadOnlyCollection<Using>(usings);
            Variables = new ReadOnlyCollection<Variable>(variables);
            PropertySetStrings = new ReadOnlyCollection<string>(propertySetStrings);
            Dependencies = new ReadOnlyCollection<Dependency>(dependencies);
        }
    }
}
