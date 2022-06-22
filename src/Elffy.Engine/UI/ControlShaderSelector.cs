#nullable enable
using Elffy.Shading;
using System;
using System.Collections;
using Elffy.Threading;

namespace Elffy.UI
{
    internal static class ControlShaderSelector
    {
        private static FastSpinLock _lock;
        private static readonly Hashtable _dic = new(); // <Type, Func<UIRenderingShader>>

        private static bool IsDebug =>
#if DEBUG
            true;
#else
            false;
#endif

        internal static void SetDefault<T>(Func<UIRenderingShader> func) where T : Control
        {
            try {
                _lock.Enter();
                _dic.Add(typeof(T), func);
            }
            finally {
                _lock.Exit();
            }
        }

        internal static UIRenderingShader GetDefault(Type controlType)
        {
            if(IsDebug) {
                CheckControlType(controlType);
            }

            Func<UIRenderingShader>? func;
            try {
                _lock.Enter();
                func = SafeCast.As<Func<UIRenderingShader>>(_dic[controlType]);
            }
            finally {
                _lock.Exit();
            }
            if(func != null) {
                return func.Invoke();
            }
            return ControlDefaultShader.Instance;
        }

        private static void CheckControlType(Type controlType)
        {
            if(controlType.IsAssignableTo(typeof(Control)) == false) {
                throw new ArgumentException($"{nameof(controlType)} must be subtype of ${nameof(Control)}.");
            }
        }
    }
}
