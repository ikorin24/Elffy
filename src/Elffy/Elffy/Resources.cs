#nullable enable
using Elffy.Exceptions;
using Elffy.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace Elffy
{
    public static class Resources
    {
        private static Dictionary<string, ResourceObject>? _resources;
        private static bool _isInitialized;
        private static ResourceLoader? _loader;
        private static string? _resourceFilePath;

        /// <summary>Get whether <see cref="Resources"/> class is initialized.</summary>
        public static bool IsInitialized => _isInitialized;

        /// <summary>Get resource loader instance</summary>
        public static ResourceLoader Loader
        {
            get
            {
                CheckInitialized();
                Debug.Assert(_loader is null == false);
                return _loader!;
            }
        }

        /// <summary>Initialize resource</summary>
        /// <param name="resourceFilePath">file path of resource</param>
        public static void Initialize(string resourceFilePath)
        {
            if(resourceFilePath is null) {
                throw new ArgumentNullException(nameof(resourceFilePath));
            }
            if(!File.Exists(resourceFilePath)) {
                throw new FileNotFoundException("Resource file not found", resourceFilePath);
            }
            if(_isInitialized) {
                throw new InvalidOperationException("Already initialized");
            }

            try {
                _resources = ResourceInitializer.CreateDictionary(resourceFilePath);
                _resourceFilePath = resourceFilePath;
                _loader = new ResourceLoader();
                _isInitialized = true;
            }
            catch(Exception ex) {
                Close();
                throw new FormatException("Failed in creating resource dic.", ex);
            }
        }

        /// <summary>Release resource dictionary</summary>
        public static void Close()
        {
            _resourceFilePath = null;
            _loader = null;
            _resources = null;
            _isInitialized = false;
        }

        /// <summary>Get <see cref="Stream"/> to load a resource</summary>
        /// <param name="name">name of the resource</param>
        /// <returns><see cref="Stream"/> to load</returns>
        public static ResourceStream GetStream(string name)
        {
            CheckInitialized();
            if(name is null) {
                ThrowNullArg();
                static void ThrowNullArg() => throw new ArgumentNullException(nameof(name));
            }

            if(_resources!.TryGetValue(name!, out var resource) == false) {
                throw new ResourceNotFoundException(name!);
            }
            Debug.Assert(_resourceFilePath is null == false);
            return new ResourceStream(_resourceFilePath, resource);
        }

        internal static string[] GetResourceNames()
        {
            // this method is only for debug

            CheckInitialized();
            return _resources!.Keys.ToArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckInitialized()
        {
            if(!_isInitialized) {
                ThrowNotInitialized();
                static void ThrowNotInitialized() => throw new InvalidOperationException("Resources not Initialized");
            }
        }
    }
}
