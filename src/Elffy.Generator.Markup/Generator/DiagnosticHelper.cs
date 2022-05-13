#nullable enable
using System;
using System.Collections.Concurrent;
using Microsoft.CodeAnalysis.Text;
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

    public static Diagnostic GeneratorInternalException(Exception ex, string filePath)
    {
        const string ID = "EGM0001";
        var diagnostic = _dicEGM.GetOrAdd(ID, key => new(
            key,
            "Generator Internal Exception",
            "Internal exception in the generator. Exception: {0}.",
            Category_Error,
            DiagnosticSeverity.Error,
            true));
        var exStr = ExceptionToString(ex);
        var location = Location.Create(filePath, new TextSpan(0, 0), new LinePositionSpan(new LinePosition(0, 0), new LinePosition(0, 0)));
        return Diagnostic.Create(diagnostic, location, exStr);
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
        return Diagnostic.Create(diagnostic, location, filePath);
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
        return Diagnostic.Create(diagnostic, location, typeName);
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
        return Diagnostic.Create(diagnostic, location, targetTypeName, memberName);
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
        return Diagnostic.Create(diagnostic, location, targetTypeName, memberName);
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
        return Diagnostic.Create(diagnostic, location, targetTypeName);
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
        return Diagnostic.Create(diagnostic, location);
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
        return Diagnostic.Create(diagnostic, location, targetTypeName);
    }

    public static Diagnostic InvalidLiteral(string literal, string literalTypeName, Location? location = null)
    {
        const string ID = "EGM0009";
        var diagnostic = _dicEGM.GetOrAdd(ID, key => new(
            key,
            "Invalid Literal",
            "Invalid Literal '{0}'. Type: {1}.",
            Category_Error,
            DiagnosticSeverity.Error,
            true));
        return Diagnostic.Create(diagnostic, location, literal, literalTypeName);
    }

    public static Diagnostic InvalidLiteralWithException(string literal, string literalTypeName, Exception ex, Location? location = null)
    {
        const string ID = "EGM0010";
        var diagnostic = _dicEGM.GetOrAdd(ID, key => new(
            key,
            "Invalid Literal",
            "Invalid Literal '{0}'. Type: {1}, Exception: {2}.",
            Category_Error,
            DiagnosticSeverity.Error,
            true));
        var exStr = ExceptionToString(ex);
        return Diagnostic.Create(diagnostic, location, literal, literalTypeName, exStr);
    }

    private static string ExceptionToString(Exception? ex)
    {
        if(ex == null) {
            return "null";
        }
        var fullName = ex.GetType().FullName;
        var message = ex.Message.Replace(Environment.NewLine, " ");
        var stacktrace = ex.StackTrace.Replace(Environment.NewLine, " ");
        return $"{fullName} {message}, {stacktrace}";
    }
}
