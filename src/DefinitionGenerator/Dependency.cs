using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DefinitionGenerator
{
    public class Dependency
    {
        public Variable Owner { get; protected set; }
        public string Property { get; protected set; }
        public Variable Value { get; protected set; }

        public Dependency(Variable owner, string property, Variable value)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            Property = property ?? throw new ArgumentNullException(nameof(property));
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }

        protected Dependency() { }

        public virtual string Dump()
        {
            return $"{Owner.Name}.{Property} = {Value.Name}";
        }
    }

    public class DependencyCollection : Dependency
    {
        public ReadOnlyCollection<Variable> Values { get; private set; }

        public DependencyCollection(IList<Dependency> dependencies)
        {
            if(dependencies == null) { throw new ArgumentNullException(nameof(dependencies)); }
            if(dependencies.Count == 0) { throw new ArgumentException(); }
            var first = dependencies[0];
            Owner = first.Owner;
            Property = first.Property;
            Values = dependencies.Select(d => d.Value).ToList().AsReadOnly();
        }

        public override string Dump()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("{}", Owner.Name);
            return "";
        }
    }
}
