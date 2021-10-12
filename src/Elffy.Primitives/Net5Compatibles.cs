#if !NET5_0_OR_GREATER
namespace System.Runtime.CompilerServices
{
    internal sealed class IsExternalInit { }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class ModuleInitializerAttribute : Attribute { }
}
#endif

#if !NET5_0_OR_GREATER
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Constructor | AttributeTargets.Event | AttributeTargets.Interface | AttributeTargets.Method | AttributeTargets.Module | AttributeTargets.Property | AttributeTargets.Struct, Inherited=false)]
    internal sealed class SkipLocalsInitAttribute : Attribute { }
}
#endif
