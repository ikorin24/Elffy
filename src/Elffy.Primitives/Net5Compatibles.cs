#if !NET5_0_OR_GREATER
namespace System.Runtime.CompilerServices
{
    internal sealed class IsExternalInit { }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class ModuleInitializerAttribute : Attribute { }
}
#endif
