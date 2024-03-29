﻿#nullable enable
using Microsoft.CodeAnalysis;

namespace ElffyGenerator
{
    internal static class DiagnosticDescriptors
    {
        private const string Category_Generator = "ElffyGenerator";

        public static readonly DiagnosticDescriptor MultiEntryPoints =
            new ("EG0001", "Cannot set multiple entry points",
                "Multiple GameEntryPointAttribute are set", 
                Category_Generator, DiagnosticSeverity.Error, true);
    }
}
