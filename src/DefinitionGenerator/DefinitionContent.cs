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
        public ReadOnlyCollection<Dependency> Dependencies { get; private set; }

        public DefinitionContent(IList<Using> usings, IList<Variable> variables, IList<Dependency> dependencies)
        {
            Usings = new ReadOnlyCollection<Using>(usings);
            Variables = new ReadOnlyCollection<Variable>(variables);
            Dependencies = new ReadOnlyCollection<Dependency>(dependencies);
        }
    }
}
