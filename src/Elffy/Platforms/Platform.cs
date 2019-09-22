using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elffy.Platforms
{
    internal static class Platform
    {
        public static PlatformType GetPlatformType()
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
