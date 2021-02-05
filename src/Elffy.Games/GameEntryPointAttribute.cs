#nullable enable
using System;

namespace Elffy
{
    /// <summary>Mark attribute of the game entry point</summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class GameEntryPointAttribute : Attribute
    {
    }
}
