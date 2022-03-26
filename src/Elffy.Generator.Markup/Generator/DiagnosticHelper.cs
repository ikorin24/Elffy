#nullable enable
using System;
using Microsoft.CodeAnalysis;

namespace Elffy.Generator;

#pragma warning disable RS2008
internal static class DiagnosticHelper
{
    private const string Category_Error = "Error";

    private static DiagnosticDescriptor? ELFGM0001;
    private static DiagnosticDescriptor? ELFGM0002;

    public static Diagnostic GeneratorInternalException(Exception ex)
    {
        ELFGM0001 ??= new(
            nameof(ELFGM0001),
            "Generator Internal Exception",
            "Internal exception in the generator. Exception: {0}.",
            Category_Error,
            DiagnosticSeverity.Error,
            true);
        return Diagnostic.Create(ELFGM0001, Location.None, ex);
    }

    public static Diagnostic InvalidXmlFormat(string filePath, Location? location = null)
    {
        ELFGM0002 ??= new(
            nameof(ELFGM0002),
            "Invalid Xml Format",
            "Xml format is invalid. File: {0}.",
            Category_Error,
            DiagnosticSeverity.Error,
            true);
        return Diagnostic.Create(ELFGM0002, location ?? Location.None, filePath);
    }
}
