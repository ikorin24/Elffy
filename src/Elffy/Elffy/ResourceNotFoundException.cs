#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Elffy
{
    public class ResourceNotFoundException : Exception
    {
        /// <summary>Target resource name.</summary>
        public string ResourceName { get; private set; }

        public ResourceNotFoundException(ResourceFile file)
        {
            ResourceName = file.Name;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfNotFound(ResourceFile file)
        {
            if(file.Exists() == false) {
                Throw(file);
            }
        }

        [DoesNotReturn]
        public static void Throw(ResourceFile file) => throw new ResourceNotFoundException(file);
    }
}
