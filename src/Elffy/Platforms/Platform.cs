#nullable enable
using System;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;

namespace Elffy.Platforms
{
    /// <summary>Provides platform information</summary>
    public static class Platform
    {
        /// <summary>Get current platform type</summary>
        public static PlatformType PlatformType { get; }

        static Platform()
        {
            PlatformType = GetPlatformType();
        }

        [DoesNotReturn]
        internal static void ThrowPlatformNotSupported()
        {
            throw new PlatformNotSupportedException($"Platform does not support this function.; Platform Type : '{PlatformType}'");
        }

        private static PlatformType GetPlatformType()
        {
            if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                return PlatformType.Windows;
            }
            else if(RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                return PlatformType.MacOSX;
            }
            else if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                return PlatformType.Linux;
            }
            else {
                return PlatformType.Other;
            }
        }
    }
}
