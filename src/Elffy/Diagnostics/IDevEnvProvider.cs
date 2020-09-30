#nullable enable
using System;
using System.Collections.Generic;
using System.Text;

namespace Elffy.Diagnostics
{
    public interface IDevEnvProvider
    {
        void Write(string? category, string message);
    }
}
