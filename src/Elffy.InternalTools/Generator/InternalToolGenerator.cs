#nullable enable
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace Elffy.Generator;

[Generator]
public sealed class InternalToolGenerator : IIncrementalGenerator
{
    private static readonly string GeneratorSigniture = GeneratorUtil.GetGeneratorSigniture(typeof(InternalToolGenerator));
    private const string SafeCastSource =
@"#nullable enable

namespace Elffy
{
/// <summary>Helper class for fast cast</summary>
internal static class SafeCast
{
    /// <summary>
    /// Cast the object to the specified type.<para/>
    /// It is similar to `(T)value`.<para/>
    /// </summary>
    /// <remarks>[NOTE] Type of the instance is checked only when 'DEBUG' build. Be careful!</remarks>
    /// <typeparam name=""T"">type to cast to</typeparam>
    /// <param name=""value"">object to cast</param>
    /// <returns>casted object</returns>
    [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    [return: global::System.Diagnostics.CodeAnalysis.NotNullIfNotNull(""value"")]
    [global::System.Diagnostics.DebuggerHidden]
    public static T? As<T>(object? value) where T : class
    {
#if DEBUG
        if(value is not null) {
            if(value is not T) {
                throw new global::System.InvalidCastException($""Cannot cast the value to '{typeof(T).FullName}': The actual value is '{value.GetType().FullName}'."");
            }
        }
#endif
        return global::System.Runtime.CompilerServices.Unsafe.As<T?>(value);
    }

    /// <summary>
    /// Check the object is not null and cast it to the specified type.<para/>
    /// It is similar to `value is not null ? (T)value : throw new ArgumentNullException()`.<para/>
    /// </summary>
    /// <remarks>[NOTE] Non-null and type checking are checked only when 'DEBUG' build. Be careful!</remarks>
    /// <typeparam name=""T"">type to cast to</typeparam>
    /// <param name=""value"">object to cast</param>
    /// <returns>casted object</returns>
    [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    [global::System.Diagnostics.DebuggerHidden]
    public static T NotNullAs<T>(object? value) where T : class
    {
#if DEBUG
        if(value is null) {
            throw new global::System.ArgumentNullException(""The value must be non-null."");
        }
        if(value is not T) {
            throw new global::System.InvalidCastException($""Cannot cast the value to '{typeof(T).FullName}': The actual value is '{value.GetType().FullName}'."");
        }
#endif
        return global::System.Runtime.CompilerServices.Unsafe.As<T>(value);
    }
}
}
";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(static context =>
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            context.AddSource("SafeCast.g.cs", SourceText.From(GeneratorSigniture + SafeCastSource, Encoding.UTF8));
        });
    }
}
