using System;
using System.Collections.Generic;
using System.Text;

namespace Sandbox
{
    internal static class AssemblyInfo
    {
        public const bool IsDebug =
#if DEBUG
            true;
#else
            false;
#endif
    }
}
