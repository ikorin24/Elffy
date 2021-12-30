#nullable enable
using System;

namespace Elffy
{
    [AttributeUsage(AttributeTargets.Struct)]
    internal sealed class NonCopyableAttribute : Attribute { }
}
