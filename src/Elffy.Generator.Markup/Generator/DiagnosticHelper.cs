#nullable enable
using System;
using Microsoft.CodeAnalysis;

namespace Elffy.Generator;

#pragma warning disable RS2008
internal static class DiagnosticHelper
{
    private const string Category_Error = "Error";

    private static readonly DiagnosticDescriptor ELFGM0001 = new(
        nameof(ELFGM0001),
        "Generator Internal Exception",
        "Internal exception in the generator. Exception: {0}.",
        Category_Error,
        DiagnosticSeverity.Error,
        true);

    public static Diagnostic GeneratorInternalException(Exception ex)
    {
        return Diagnostic.Create(ELFGM0001, Location.None, ex);
    }
}
