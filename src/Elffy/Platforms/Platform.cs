#nullable enable
using System;

namespace Elffy.Platforms
{
    /// <summary>プラットフォームに関する機能を提供します</summary>
    public static class Platform
    {
        /// <summary>現在のプラットフォームの種類を取得します</summary>
        public static PlatformType PlatformType { get; }

        static Platform()
        {
            PlatformType = GetPlatformType();
        }

        /// <summary>特定の機能がこのプラットフォームでサポートされないことを示す例外を取得します</summary>
        /// <returns><see cref="PlatformNotSupportedException"/> のインスタンス</returns>
        public static PlatformNotSupportedException PlatformNotSupported() 
            => new PlatformNotSupportedException($"Platform does not support this function.; Platform Type : '{PlatformType}'");

        private static PlatformType GetPlatformType()
        {
            switch(Environment.OSVersion.Platform) {
                case PlatformID.Win32NT:
                case PlatformID.Win32Windows:
                case PlatformID.WinCE:
                case PlatformID.Win32S:
                    return PlatformType.Windows;
                case PlatformID.MacOSX:
                    return PlatformType.MacOSX;
                case PlatformID.Unix:
                    return PlatformType.Unix;
                default:
                    return PlatformType.Other;
            }
        }
    }
}
