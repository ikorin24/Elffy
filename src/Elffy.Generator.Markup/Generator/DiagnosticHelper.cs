#nullable enable
using System;
using System.Collections.Concurrent;
using Microsoft.CodeAnalysis;

namespace Elffy.Generator;

#pragma warning disable RS2008
internal static class DiagnosticHelper
{
    private const string Category_Error = "Error";
    private static readonly ConcurrentDictionary<string, DiagnosticDescriptor> _dicEGM = new();

    public static Diagnostic LanguageNotSupported(string language)
    {
        const string ID = "EGM0000";
        var diagnostic = _dicEGM.GetOrAdd(ID, key => new(
            key,
            "Language Not Supported",
            "C# is a only supported language. Language: {0}.",
            Category_Error,
            DiagnosticSeverity.Error,
            true));
        return Diagnostic.Create(diagnostic, Location.None, language);
    }

    public static Diagnostic GeneratorInternalException(Exception ex)
    {
        const string ID = "EGM0001";
        var diagnostic = _dicEGM.GetOrAdd(ID, key => new(
            key,
            "Generator Internal Exception",
            "Internal exception in the generator. Exception: {0}, StackTrace: {1}.",
            Category_Error,
            DiagnosticSeverity.Error,
            true));
        var message = ex.Message.Replace(Environment.NewLine, "\\n");
        var stacktrace = ex.StackTrace.Replace(Environment.NewLine, "\\n");
        return Diagnostic.Create(diagnostic, Location.None, message, stacktrace);
    }

    public static Diagnostic InvalidXmlFormat(string filePath, Location? location = null)
    {
        const string ID = "EGM0002";
        var diagnostic = _dicEGM.GetOrAdd(ID, key => new(
            key,
            "Invalid Xml Format",
            "Xml format is invalid. File: {0}.",
            Category_Error,
            DiagnosticSeverity.Error,
            true));
        return Diagnostic.Create(diagnostic, location ?? Location.None, filePath);
    }

    public static Diagnostic TypeNotFound(string typeName, Location? location = null)
    {
        const string ID = "EGM0003";
        var diagnostic = _dicEGM.GetOrAdd(ID, key => new(
            key,
            "Type Not Found",
            "Type is not found. Type: {0}.",
            Category_Error,
            DiagnosticSeverity.Error,
            true));
        return Diagnostic.Create(diagnostic, location ?? Location.None, typeName);
    }

    public static Diagnostic SettableMemberNotFound(string targetTypeName, string memberName, Location? location = null)
    {
        const string ID = "EGM0004";
        var diagnostic = _dicEGM.GetOrAdd(ID, key => new(
            key,
            "Settable Member Not Found",
            "Type '{0}' has not settable member '{1}'",
            Category_Error,
            DiagnosticSeverity.Error,
            true));
        return Diagnostic.Create(diagnostic, location ?? Location.None, targetTypeName, memberName);
    }

    public static Diagnostic MutipleValuesNotSupported(string targetTypeName, string memberName, Location? location = null)
    {
        const string ID = "EGM0005";
        var diagnostic = _dicEGM.GetOrAdd(ID, key => new(
            key,
            "Mutiple Values Not Supported",
            "Cannot set multiple values. Type: {0}, Member: {1}.",
            Category_Error,
            DiagnosticSeverity.Error,
            true));
        return Diagnostic.Create(diagnostic, location ?? Location.None, targetTypeName, memberName);
    }

    public static Diagnostic DirectContentNotSupported(string targetTypeName, Location? location = null)
    {
        const string ID = "EGM0006";
        var diagnostic = _dicEGM.GetOrAdd(ID, key => new(
            key,
            "Direct Content Not Supported",
            "Cannot set value without specifying the target settable member. Type: {0}.",
            Category_Error,
            DiagnosticSeverity.Error,
            true));
        return Diagnostic.Create(diagnostic, location ?? Location.None, targetTypeName);
    }

    public static Diagnostic BuilderNotSpecified(Location? location = null)
    {
        const string ID = "EGM0007";
        var diagnostic = _dicEGM.GetOrAdd(ID, key => new(
            key,
            "Builder Not Specified",
            "Builder is not specified in the root node",
            Category_Error,
            DiagnosticSeverity.Error,
            true));
        return Diagnostic.Create(diagnostic, location ?? Location.None);
    }

    public static Diagnostic LiteralValueNotSupported(string targetTypeName, Location? location = null)
    {
        const string ID = "EGM0008";
        var diagnostic = _dicEGM.GetOrAdd(ID, key => new(
            key,
            "Literal Value Not Supported",
            "Target type does not support literal value. Type: {0}.",
            Category_Error,
            DiagnosticSeverity.Error,
            true));
        return Diagnostic.Create(diagnostic, location ?? Location.None, targetTypeName);
    }

    public static Diagnostic CannotCreateValueFromLiteral(string literal, string literalTypeName, Exception? exception, Location? location = null)
    {
        const string ID = "EGM0009";
        var diagnostic = _dicEGM.GetOrAdd(ID, key => new(
            key,
            "Cannot Create Value from Literal",
            "Cannot create value from specified literal. Literal: {0}, Type: {1}, Exception: {2}.",
            Category_Error,
            DiagnosticSeverity.Error,
            true));
        return Diagnostic.Create(diagnostic, location ?? Location.None, literal, literalTypeName, exception);
    }
}
