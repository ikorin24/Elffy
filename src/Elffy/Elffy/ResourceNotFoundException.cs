#nullable enable
using System;

namespace Elffy
{
    public class ResourceNotFoundException : Exception
    {
        /// <summary>Target resource name.</summary>
        public string ResourceName { get; private set; }

        public ResourceNotFoundException(string resourceName) : base($"Resource not found : '{resourceName}'")
        {
            ResourceName = resourceName;
        }
    }
}
