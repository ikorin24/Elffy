#nullable enable
using System;
using System.Threading;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Elffy.Exceptions;

namespace Elffy
{
    /// <summary>Provides resource loader</summary>
    public static class Resources
    {
        private static IResourceLoader _loader = EmptyResourceLoader.Instance;

        /// <summary>Get resource loader instance</summary>
        public static IResourceLoader Loader => _loader;

        public static void Inject(IResourceLoader loader)
        {
            if(loader is null) {
                ThrowNullArg();
                [DoesNotReturn] static void ThrowNullArg() => throw new ArgumentNullException(nameof(loader));
            }
            if(Interlocked.CompareExchange(ref _loader, loader, EmptyResourceLoader.Instance) != EmptyResourceLoader.Instance) {
                ThrowAlreadyInitialized();
            }
        }

        /// <summary>Inject a <see cref="IResourceLoader"/> instance by a factory.</summary>
        /// <param name="loaderFactory">factory function of <see cref="IResourceLoader"/> instance.</param>
        public static void Inject(Func<IResourceLoader> loaderFactory)
        {
            if(loaderFactory is null) {
                ThrowNullArg(nameof(loaderFactory));
            }
            if(Interlocked.CompareExchange(ref _loader, loaderFactory(), EmptyResourceLoader.Instance) != EmptyResourceLoader.Instance) {
                ThrowAlreadyInitialized();
            }
        }

        /// <summary>Inject a <see cref="IResourceLoader"/> instance by a factory with spacified arg.</summary>
        /// <typeparam name="T">arg type</typeparam>
        /// <param name="loaderFactory">factory function of <see cref="IResourceLoader"/> instance.</param>
        /// <param name="arg">factory arg</param>
        public static void Inject<T>(Func<T, IResourceLoader> loaderFactory, T arg)
        {
            if(loaderFactory is null) {
                ThrowNullArg(nameof(loaderFactory));
            }
            if(Interlocked.CompareExchange(ref _loader, loaderFactory(arg), EmptyResourceLoader.Instance) != EmptyResourceLoader.Instance) {
                ThrowAlreadyInitialized();
            }
        }

        /// <summary>Release <see cref="IResourceLoader"/> instance.</summary>
        public static void Close()
        {
            _loader = EmptyResourceLoader.Instance;
        }

        [DoesNotReturn]
        private static void ThrowNullArg(string name) => throw new ArgumentNullException(name);

        [DoesNotReturn]
        private static void ThrowAlreadyInitialized() => throw new InvalidOperationException("Resources is already initialized.");

        private sealed class EmptyResourceLoader : IResourceLoader
        {
            public static readonly IResourceLoader Instance = new EmptyResourceLoader();

            private EmptyResourceLoader()
            {
            }

            public long GetSize(string name) => throw new ResourceNotFoundException(name);

            public Stream GetStream(string name) => throw new ResourceNotFoundException(name);

            public bool HasResource(string name) => false;
        }
    }
}
