#nullable enable
using System;

namespace Elffy
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
    public sealed class DefineLocalResourceAttribute : Attribute
    {
        public DefineLocalResourceAttribute(string importedName, string packageFilePath)
        {
        }
    }
}
