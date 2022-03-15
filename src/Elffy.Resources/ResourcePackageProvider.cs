#nullable enable
using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Elffy
{
    [EditorBrowsable(EditorBrowsableState.Never)]   // This class is intended for use only from Source Generator
    public static class ResourcePackageProvider
    {
        private static readonly ConcurrentDictionary<string, IResourcePackage> _registeredPackage = new();

        public static IResourcePackage CreateLocalResourcePackage(string packageName, string resourcePackageFilePath)
        {
            var package = new LocalResourcePackage(packageName, resourcePackageFilePath);
            return package;
        }

        public static void RegisterPublicPackage(string packageId, IResourcePackage package)
        {
            ArgumentNullException.ThrowIfNull(packageId);
            ArgumentNullException.ThrowIfNull(package);
            if(string.IsNullOrWhiteSpace(packageId)) {
                ThrowInvalidPackageId();
                [DoesNotReturn] static void ThrowInvalidPackageId() => throw new ArgumentException($"Package ID is empty or whitespace.");
            }

            if(_registeredPackage.TryAdd(packageId, package) == false) {
                ThrowDuplicatedPackageId(packageId);
                [DoesNotReturn] static void ThrowDuplicatedPackageId(string packageId) => throw new ArgumentException($"Package ID is duplicatred. (Package ID: '{packageId}')");
            }
        }

        public static IResourcePackage GetResourcePackage(string packageId)
        {
            ArgumentNullException.ThrowIfNull(packageId);
            if(_registeredPackage.TryGetValue(packageId, out var package) == false) {
                ThrowPackageNotFound(packageId);
                [DoesNotReturn] static void ThrowPackageNotFound(string packageId) => throw new ArgumentException($"Package is not found. (Package ID: '{packageId}')");
            }
            return package;
        }
    }
}
