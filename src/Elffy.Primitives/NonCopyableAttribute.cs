#nullable enable
using System;

namespace Elffy
{
    [AttributeUsage(AttributeTargets.Struct)]
    public sealed class NonCopyableAttribute : Attribute { }
}
