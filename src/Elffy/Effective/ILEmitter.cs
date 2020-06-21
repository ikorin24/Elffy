#nullable enable
using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Elffy.Effective
{
    /// <summary>
    /// IL generator helper class<para/>
    /// [NOTE]<para/>
    /// Methods this helper class generate are instance method, not static because of performance on calling delegate.<para/>
    /// HOWEVER, arg0 (as instance) is null because the instance is useless, so 'ldarg.0' loads null to stack.<para/>
    /// </summary>
    public static class ILEmitter
    {
        private static readonly AssemblyBuilder _assemblyBuilder;
        private static readonly ModuleBuilder _moduleBuilder;

        private const string AssemblyName = "DynamicAssembly";
        private const string ModuleName = "DynamicModule";
        private static int _typeNum;
        private const string MethodName = "M";


        // TypeBuilder.DefineMethod method requires Type[] instance.
        // Use cache array to avoid allocation.
        private static Type[]? _arg1;
        private static Type[]? _arg2;

        static ILEmitter()
        {
            _assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(AssemblyName), AssemblyBuilderAccess.Run);
            _moduleBuilder = _assemblyBuilder.DefineDynamicModule(ModuleName);
        }

        /// <summary>
        /// Emit <see cref="Action"/> delegate.<para/>
        /// [NOTE] See class comment for remarking.<para/>
        /// </summary>
        /// <param name="emitter">Method body emitter</param>
        /// <returns><see cref="Action"/> delegate</returns>
        public static Action EmitAction(Action<ILGenerator> emitter)
        {
            return Emit<Action>(emitter, null, null);
        }

        /// <summary>
        /// Emit <see cref="Action{T}"/> delegate.<para/>
        /// [NOTE] See class comment for remarking.<para/>
        /// </summary>
        /// <param name="emitter">Method body emitter</param>
        /// <returns><see cref="Action{T}"/> delegate</returns>
        public static Action<TArg> EmitAction1<TArg>(Action<ILGenerator> emitter)
        {
            _arg1 ??= new Type[1];
            _arg1[0] = typeof(TArg);
            return Emit<Action<TArg>>(emitter, null, _arg1);
        }

        /// <summary>
        /// Emit <see cref="Action{T1, T2}"/> delegate.<para/>
        /// [NOTE] See class comment for remarking.<para/>
        /// </summary>
        /// <param name="emitter">Method body emitter</param>
        /// <returns><see cref="Action{T1, T2}"/> delegate</returns>
        public static Action<TArg1, TArg2> EmitAction2<TArg1, TArg2>(Action<ILGenerator> emitter)
        {
            _arg2 ??= new Type[2];
            _arg2[0] = typeof(TArg1);
            _arg2[1] = typeof(TArg2);
            return Emit<Action<TArg1, TArg2>>(emitter, null, _arg2);
        }

        /// <summary>
        /// Emit <see cref="Func{TResult}"/> delegate.<para/>
        /// [NOTE] See class comment for remarking.<para/>
        /// </summary>
        /// <param name="emitter">Method body emitter</param>
        /// <returns><see cref="Func{TResult}"/> delegate</returns>
        public static Func<TRet> EmitFunc<TRet>(Action<ILGenerator> emitter)
        {
            return Emit<Func<TRet>>(emitter, typeof(TRet), null);
        }

        /// <summary>
        /// Emit <see cref="Func{T, TResult}"/> delegate.<para/>
        /// [NOTE] See class comment for remarking.<para/>
        /// </summary>
        /// <param name="emitter">Method body emitter</param>
        /// <returns><see cref="Func{T,TResult}"/> delegate</returns>
        public static Func<TArg, TRet> EmitFunc1<TArg, TRet>(Action<ILGenerator> emitter)
        {
            _arg1 ??= new Type[1];
            _arg1[0] = typeof(TArg);
            return Emit<Func<TArg, TRet>>(emitter, typeof(TRet), _arg1);
        }

        /// <summary>
        /// Emit <see cref="Func{T1, T2, TResult}"/> delegate.<para/>
        /// [NOTE] See class comment for remarking.<para/>
        /// </summary>
        /// <param name="emitter">Method body emitter</param>
        /// <returns><see cref="Func{T1, T2, TResult}"/> delegate</returns>
        public static Func<TArg1, TArg2, TRet> EmitFunc2<TArg1, TArg2, TRet>(Action<ILGenerator> emitter)
        {
            _arg2 ??= new Type[2];
            _arg2[0] = typeof(TArg1);
            _arg2[1] = typeof(TArg2);
            return Emit<Func<TArg1, TArg2, TRet>>(emitter, typeof(TRet), _arg2);
        }



        private static TDelegate Emit<TDelegate>(Action<ILGenerator> emitter, Type? retType, Type[]? argType) where TDelegate : Delegate
        {
            // Generated method is instance method, not static because of performance on calling delegate.
            // HOWEVER, arg0 (as instance) is null because the instance is useless.

            // Type name is "0", "1", "2", ...
            var typeName = (_typeNum++).ToString();

            var typeBuilder = _moduleBuilder.DefineType(typeName, TypeAttributes.Public | TypeAttributes.Sealed, typeof(object))!;
            var methodBuilder = typeBuilder.DefineMethod(MethodName, MethodAttributes.Public, retType, argType);

            // Method body is emitted here.
            emitter(methodBuilder.GetILGenerator());

            var method = typeBuilder.CreateTypeInfo()!
                                    .AsType()
                                    .GetMethod(MethodName, BindingFlags.Public | BindingFlags.Instance)!;

            return (TDelegate)method.CreateDelegate(typeof(TDelegate), null);
        }
    }
}
