using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elffy.Exceptions
{
    public class ResourceNotFoundException : Exception
    {
        /// <summary>Target resource name.</summary>
        public string ResourceName { get; private set; }

        internal ResourceNotFoundException(string resourceName) : base("Resource not found")
        {
            ResourceName = resourceName;
        }
    }
}
