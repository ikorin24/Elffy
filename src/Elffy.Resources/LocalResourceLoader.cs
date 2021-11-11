#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Elffy
{
    internal sealed class LocalResourceLoader : IResourceLoader
    {
        private readonly string _resourcePackageFilePath;
        private readonly Dictionary<string, ResourceObject> _resources;

        internal string ResourcePackageFilePath => _resourcePackageFilePath;

        public LocalResourceLoader(string resourcePackageFilePath)
        {
            if(resourcePackageFilePath is null) { throw new ArgumentNullException(nameof(resourcePackageFilePath)); }

            _resourcePackageFilePath = Path.Combine(AppContext.BaseDirectory, resourcePackageFilePath);
            _resources = LocalResourceInitializer.CreateDictionary(_resourcePackageFilePath);
        }

        // This method is only for debug.
        internal string[] GetResourceNames()
        {
            return _resources.Keys.ToArray();
        }

        public bool TryGetStream(string? name, out Stream stream)
        {
            if(name is null || _resources.TryGetValue(name, out var resource) == false) {
                stream = Stream.Null;
                return false;
            }
            stream = new LocalResourceStream(_resourcePackageFilePath, resource);
            return true;
        }

        public bool TryGetSize(string? name, out long size)
        {
            if(name is null || _resources.TryGetValue(name, out var resource) == false) {
                size = 0;
                return false;
            }
            size = resource.Length;
            return true;
        }

        public bool Exists(string? name)
        {
            return name is not null && _resources.ContainsKey(name);
        }
    }

    internal sealed class EmptyResourceLoader : IResourceLoader
    {
        private static readonly EmptyResourceLoader _instance = new EmptyResourceLoader();
        public static EmptyResourceLoader Null => _instance;

        public bool Exists(string name) => false;

        public bool TryGetSize(string name, out long size)
        {
            size = 0;
            return false;
        }

        public bool TryGetStream(string name, out Stream stream)
        {
            stream = Stream.Null;
            return false;
        }
    }
}
