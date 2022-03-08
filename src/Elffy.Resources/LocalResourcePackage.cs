#nullable enable
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Elffy
{
    internal sealed class LocalResourcePackage : IResourcePackage
    {
        private readonly string _name;
        private readonly string _resourcePackageFilePath;
        private readonly Dictionary<string, ResourceObject> _resources;

        internal string ResourcePackageFilePath => _resourcePackageFilePath;

        public string Name => _name;

        public LocalResourcePackage(string packageName, string resourcePackageFilePath)
        {
            ArgumentNullException.ThrowIfNull(packageName);
            ArgumentNullException.ThrowIfNull(resourcePackageFilePath);

            _resourcePackageFilePath = Path.Combine(AppContext.BaseDirectory, resourcePackageFilePath);
            _resources = LocalResourceInitializer.CreateDictionary(_resourcePackageFilePath);
            _name = packageName;
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

        public bool TryGetHandle(string? name, out ResourceFileHandle handle)
        {
            if(name is null || _resources.TryGetValue(name, out var resource) == false) {
                handle = ResourceFileHandle.None;
                return false;
            }
            var fileHandle = File.OpenHandle(_resourcePackageFilePath, FileMode.Open, FileAccess.Read);
            handle = new ResourceFileHandle(fileHandle, resource.Position, resource.Length);
            return true;
        }
    }
}
