#nullable enable
using Microsoft.CodeAnalysis;

namespace Elffy.Generator
{
    internal static class DiagnosticDescriptors
    {
        private const string Category_Generator = "Elffy.Generator";

        public static readonly DiagnosticDescriptor MultiEntryPoints =
            new ("EG0001", "Cannot set multiple entry points",
                "Multiple GameEntryPointAttribute are set", 
                Category_Generator, DiagnosticSeverity.Error, true);
    }
}
