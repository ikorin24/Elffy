#nullable enable
using System;

namespace Elffy.Diagnostics
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class NonCheckingShaderTargetVertexTypeAttribute : Attribute
    {
    }
}
