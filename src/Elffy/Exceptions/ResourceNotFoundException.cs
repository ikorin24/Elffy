#nullable enable
using System;

namespace Elffy.Exceptions
{
    public class ResourceNotFoundException : Exception
    {
        /// <summary>Target resource name.</summary>
        public string ResourceName { get; private set; }

        internal ResourceNotFoundException(string resourceName) : base($"Resource not found : '{resourceName}'")
        {
            ResourceName = resourceName;
        }
    }
}
