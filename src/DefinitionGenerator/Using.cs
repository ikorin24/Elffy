using System;

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
