#nullable enable
using Elffy.Exceptions;
using System;
using System.Reflection;
using System.Threading;

namespace Elffy.Core
{
    /// <summary>アプリケーションのプロセスに関する処理を提供します</summary>
    public static class ProcessHelper
    {
        /// <summary>名前付き Mutex の名前の最大長</summary>
        private const int MUTEX_NAME_MAX_LEN = 259;

        private static void DoNothing() { }

        /// <summary>Mutex を用いてアプリケーションの多重起動を防止します</summary>
        /// <param name="startAction">起動する処理</param>
        public static void SingleLaunch(Action startAction) => SingleLaunch(startAction, DoNothing);

        /// <summary>Mutex を用いてアプリケーションの多重起動を防止します</summary>
        /// <param name="startAction">起動する処理</param>
        /// <param name="multiLaunch">多重起動時に実行される処理</param>
        public static void SingleLaunch(Action startAction, Action multiLaunch)
        {
            var assemblyName = Assembly.GetEntryAssembly().GetName();
            var uniqueName = $"{assemblyName.Name}-{assemblyName.Version.ToString()}";      // 最低1文字 (Empty では Mutex が機能しないため)
            if(uniqueName.Length > MUTEX_NAME_MAX_LEN) {
                uniqueName = uniqueName.Substring(0, MUTEX_NAME_MAX_LEN);                   // 名前付きMutexの名前は文字数制限があるため
            }
            SingleLaunch(uniqueName, startAction, multiLaunch);
        }

        /// <summary>Mutex を用いてアプリケーションの多重起動を防止します</summary>
        /// <param name="uniqueName">Mutex の識別名 (他のアプリケーションと重複せず、このアプリケーションを表す固有名)</param>
        /// <param name="startAction">起動する処理</param>
        public static void SingleLaunch(string uniqueName, Action startAction) => SingleLaunch(uniqueName, startAction, DoNothing);

        /// <summary>Mutex を用いてアプリケーションの多重起動を防止します</summary>
        /// <param name="uniqueName">Mutex の識別名 (他のアプリケーションと重複せず、このアプリケーションを表す固有名)</param>
        /// <param name="startAction">起動する処理</param>
        /// <param name="multiLaunch">多重起動時に実行される処理</param>
        public static void SingleLaunch(string uniqueName, Action startAction, Action multiLaunch)
        {
            ArgumentChecker.ThrowArgumentIf(string.IsNullOrEmpty(uniqueName), $"{nameof(uniqueName)} is null or empty.");
            ArgumentChecker.ThrowIfNullArg(startAction, nameof(startAction));
            ArgumentChecker.ThrowIfNullArg(multiLaunch, nameof(multiLaunch));
            if(uniqueName.Length > MUTEX_NAME_MAX_LEN) {
                throw new ArgumentException($"{nameof(uniqueName)} is too long. Length must be between 0 and {MUTEX_NAME_MAX_LEN}.");
            }
            Mutex? mutex = null;
            try {
                mutex = new Mutex(true, uniqueName, out var createdNew);
                if(createdNew) {
                    startAction();
                }
                else {
                    multiLaunch();
                }
            }
            finally {
                mutex?.ReleaseMutex();
            }
        }
    }
}
