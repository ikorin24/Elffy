#nullable enable
using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Elffy.Core.OpenTK
{
    internal static class OpenTKHelper
    {
        private static Action? _GLFWProvider_EnsureInitialized;

        /// <summary>
        /// Call 'OpenTK.Windowing.Desktop.GLFWProvider.EnsureInitialized()', which is internal method.
        /// </summary>
        public static void GLFWProvider_EnsureInitialized()
        {
            var action = _GLFWProvider_EnsureInitialized;
            if(action is null) {
                action = _GLFWProvider_EnsureInitialized = Build();
            }
            action();

            static Action Build()
            {
                var dm = new DynamicMethod("CallEnsureInitialized",
                                       MethodAttributes.Public | MethodAttributes.Static,
                                       CallingConventions.Standard,
                                       null, null,
                                       typeof(OpenTKHelper).Module,
                                       true);

                const string AsmName = "OpenTK.Windowing.Desktop";
                const string TypeName = "OpenTK.Windowing.Desktop.GLFWProvider";
                const string MethodName = "EnsureInitialized";

                var method = Assembly.Load(AsmName)?.GetType(TypeName)?.GetMethod(MethodName)
                    ?? throw new Exception($"Cannot find method: {TypeName}.{MethodName} in assembly of {AsmName}.");

                var il = dm.GetILGenerator();
                il.Emit(OpCodes.Call, method);
                il.Emit(OpCodes.Ret);

                return (Action)dm.CreateDelegate(typeof(Action));
            }
        }
    }
}
