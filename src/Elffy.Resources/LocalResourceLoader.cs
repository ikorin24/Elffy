﻿#nullable enable
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

        public Stream GetStream(string name)
        {
            if(_resources!.TryGetValue(name, out var resource) == false) {
                ResourceNotFoundException.Throw(new ResourceFile(this, name));
            }
            return new LocalResourceStream(_resourcePackageFilePath, resource);
        }

        public long GetSize(string name)
        {
            if(_resources.TryGetValue(name, out var resource) == false) {
                ResourceNotFoundException.Throw(new ResourceFile(this, name));
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

    internal sealed class EmptyResourceLoader : IResourceLoader
    {
        private static readonly EmptyResourceLoader _instance = new EmptyResourceLoader();
        public static EmptyResourceLoader Null => _instance;
        public long GetSize(string name) => 0L;

        public Stream GetStream(string name) => Stream.Null;

        public bool HasResource(string name) => false;
    }
}