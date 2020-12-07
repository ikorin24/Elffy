#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;

namespace Elffy
{
    public static class Resources
    {
        private static IResourceLoader? _loader;

        /// <summary>Get resource loader instance</summary>
        public static IResourceLoader Loader
        {
            get
            {
                if(_loader is null) {
                    ThrowNotInitialized();
                    [DoesNotReturn] void ThrowNotInitialized() => throw new InvalidOperationException("Resources is not initialized.");
                }
                return _loader;
            }
        }

        /// <summary>Inject a <see cref="IResourceLoader"/> instance by a factory.</summary>
        /// <param name="loaderFactory">factory function of <see cref="IResourceLoader"/> instance.</param>
        public static void Initialize(Func<IResourceLoader> loaderFactory)
        {
            if(loaderFactory is null) {
                ThrowNullArg();
                 [DoesNotReturn] static void ThrowNullArg() => throw new ArgumentNullException(nameof(loaderFactory));
            }
            _loader = loaderFactory();
        }

        /// <summary>Inject a <see cref="IResourceLoader"/> instance by a factory with spacified arg.</summary>
        /// <typeparam name="T">arg type</typeparam>
        /// <param name="loaderFactory">factory function of <see cref="IResourceLoader"/> instance.</param>
        /// <param name="arg">factory arg</param>
        public static void Initialize<T>(Func<T, IResourceLoader> loaderFactory, T arg)
        {
            if(loaderFactory is null) {
                ThrowNullArg();
                [DoesNotReturn] static void ThrowNullArg() => throw new ArgumentNullException(nameof(loaderFactory));
            }
            _loader = loaderFactory(arg);
        }

        /// <summary>Release <see cref="IResourceLoader"/> instance.</summary>
        public static void Close()
        {
            _loader = null;
        }
    }
}
