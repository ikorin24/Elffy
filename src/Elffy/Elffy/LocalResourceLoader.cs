﻿#nullable enable
using Elffy.Core;
using Elffy.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Elffy
{
    public sealed class LocalResourceLoader : IResourceLoader
    {
        private readonly string _resourcesFilePath;
        private readonly Dictionary<string, ResourceObject> _resources;

        public LocalResourceLoader(string resourcesFilePath)
        {
            if(resourcesFilePath is null) { throw new ArgumentNullException(nameof(resourcesFilePath)); }
            _resourcesFilePath = resourcesFilePath;
            _resources = ResourceInitializer.CreateDictionary(resourcesFilePath);
        }

        public Stream GetStream(string name)
        {
            if(name is null) {
                ThrowNullArg();
                static void ThrowNullArg() => throw new ArgumentNullException(nameof(name));
            }

            if(_resources!.TryGetValue(name!, out var resource) == false) {
                throw new ResourceNotFoundException(name!);
            }
            return new ResourceStream(_resourcesFilePath, resource);
        }

        // This method is only for debug.
        internal string[] GetResourceNames()
        {
            return _resources.Keys.ToArray();
        }
    }
}