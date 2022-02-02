#nullable enable
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Text;

namespace Elffy.Generator
{
    [Generator]
    public class InternalToolGenerator : ISourceGenerator
    {
        private static readonly string GeneratorSigniture = GeneratorUtil.GetGeneratorSigniture(typeof(InternalToolGenerator));

        private const string SafeCastSource =
@"#nullable enable
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;

namespace Elffy
{
    /// <summary>Helper class for fast cast</summary>
    internal static class SafeCast
    {
        /// <summary>
        /// Cast the object to the specified type.<para/>
        /// It is similar to `(T)value`.<para/>
        /// </summary>
        /// <remarks>[NOTE] Type checking is checked only when 'DEBUG' build. Be careful!</remarks>
        /// <typeparam name=""T"">type to cast to</typeparam>
        /// <param name=""value"">object to cast</param>
        /// <returns>casted object</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [return: NotNullIfNotNull(""value"")]
        [DebuggerHidden]
        public static T? As<T>(object? value) where T : class
        {
#if DEBUG
            if(value is not null) {
                if(value is not T) {
                    throw new System.InvalidCastException($""Cannot cast the value to '{typeof(T).FullName}': The actual value is '{value.GetType().FullName}'."");
                }
            }
#endif
            return Unsafe.As<T?>(value);
        }

        /// <summary>
        /// Check the object is not null and cast it to the specified type.<para/>
        /// It is similar to `value is not null ? (T)value : throw new ArgumentNullException()`.<para/>
        /// </summary>
        /// <remarks>[NOTE] Non-null and type checking are checked only when 'DEBUG' build. Be careful!</remarks>
        /// <typeparam name=""T"">type to cast to</typeparam>
        /// <param name=""value"">object to cast</param>
        /// <returns>casted object</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden]
        public static T NotNullAs<T>(object? value) where T : class
        {
#if DEBUG
            if(value is null) {
                throw new System.ArgumentNullException(""The value must be non-null."");
            }
            if(value is not T) {
                throw new System.InvalidCastException($""Cannot cast the value to '{typeof(T).FullName}': The actual value is '{value.GetType().FullName}'."");
            }
#endif
            return Unsafe.As<T>(value);
        }
    }
}
";

        public void Execute(GeneratorExecutionContext context)
        {
            if(context.Compilation.Language != "C#") {
                throw new NotSupportedException("The generator only supports C#.");
            }
            context.AddSource("SafeCast", SourceText.From(GeneratorSigniture + SafeCastSource, Encoding.UTF8));
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            // nop
        }
    }
}
