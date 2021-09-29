#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Elffy
{
    public class ResourceNotFoundException : Exception
    {
        private readonly ResourceFile _file;

        /// <summary>Target resource file name.</summary>
        public string ResourceName => _file.Name;

        public IResourceLoader ResourceLoader => _file.ResourceLoader;

        public ResourceNotFoundException(ResourceFile file) : this(file, CreateMessage(file))
        {
        }

        public ResourceNotFoundException(ResourceFile file, string? message) : base(message)
        {
            _file = file;
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

        private static string CreateMessage(ResourceFile file)
        {
            if(string.IsNullOrEmpty(file.Name)) {
                return "Invalid resource file ! The name is empty.";
            }
            if(file.ResourceLoader is LocalResourceLoader localResourceLoader) {
                return $"\"{file.Name}\" (in {localResourceLoader.ResourcePackageFilePath})";
            }
            return $"\"{file.Name}\"";
        }
    }
}
