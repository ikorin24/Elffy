#nullable enable
using Elffy.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Elffy.Core
{
    public sealed class LocalResourceLoader : IResourceLoader
    {
        private readonly string _resourcesFilePath;
        private readonly Dictionary<string, ResourceObject> _resources;

        public LocalResourceLoader(string resourcesFilePath)
        {
            if(resourcesFilePath is null) { throw new ArgumentNullException(nameof(resourcesFilePath)); }

            _resourcesFilePath = Path.Combine(AppContext.BaseDirectory, resourcesFilePath);
            _resources = LocalResourceInitializer.CreateDictionary(_resourcesFilePath);
        }

        public Stream GetStream(string name)
        {
            if(_resources!.TryGetValue(name, out var resource) == false) {
                ThrowNotFound(name);
                static void ThrowNotFound(string name) => throw new ResourceNotFoundException(name);
            }
            return new LocalResourceStream(_resourcesFilePath, resource);
        }

        public long GetSize(string name)
        {
            if(_resources!.TryGetValue(name!, out var resource) == false) {
                throw new ResourceNotFoundException(name!);
            }
            return resource.Length;
        }

        public bool HasResource(string name)
        {
            return _resources.ContainsKey(name);
        }

        // This method is only for debug.
        internal string[] GetResourceNames()
        {
            return _resources.Keys.ToArray();
        }
    }
}
