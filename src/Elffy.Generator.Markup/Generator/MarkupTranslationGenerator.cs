#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using Elffy.Markup;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using U8Xml;
using System.IO;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace Elffy.Generator;

[Generator]
public sealed class MarkupTranslationGenerator : IIncrementalGenerator
{
    private const string MarkupFileExt = ".e.xml";
    private const string OutputSourceExt = ".g.cs";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var results = context
            .AdditionalTextsProvider
            .Where(x => x.Path.EndsWith(MarkupFileExt, StringComparison.OrdinalIgnoreCase))
            .Select((x, ct) =>
            {
                ct.ThrowIfCancellationRequested();
                return new MarkupFile(x.Path, MarkupFile.ComputeFileHash(x.Path));
            })
            .WithComparer(MarkupFileEqualityComparer.Default)
            .Combine(context.CompilationProvider)
            .Select(static (x, ct) =>
            {
                var result = new MarkupTranslationResult();
                try {
                    ct.ThrowIfCancellationRequested();
                    var (file, compilation) = x;
                    if(compilation.Language != "C#") {
                        result.AddDiagnostic(DiagnosticHelper.LanguageNotSupported(compilation.Language));
                        return result;
                    }
                    var filePath = file.FilePath;
                    XmlObject xml;
                    try {
                        xml = XmlParser.ParseFile(filePath);
                    }
                    catch(FormatException) {
                        DiagnosticHelper.InvalidXmlFormat(filePath);
                        return result;
                    }
                    var typeInfoStore = RoslynTypeInfoStore.Create(xml, compilation, ct);
                    var outputName = Path.GetFileNameWithoutExtension(filePath) + OutputSourceExt;
                    result.SetOutputName(outputName);

                    MarkupTranslator.Translate(xml, typeInfoStore, result, ct);
                }
                catch(Exception ex) when(ex is not OperationCanceledException) {
                    result.AddDiagnostic(DiagnosticHelper.GeneratorInternalException(ex));
                }
                return result;
            });

        context.RegisterSourceOutput(results, static (context, result) =>
        {
            try {
                context.CancellationToken.ThrowIfCancellationRequested();
                result.ReportDiagnosticTo(context);
                if(result.TryGetResult(out var outputName, out var sourceText)) {
                    context.AddSource(outputName, sourceText);
                }
            }
            catch(Exception ex) when(ex is not OperationCanceledException) {
                context.ReportDiagnostic(DiagnosticHelper.GeneratorInternalException(ex));
            }
        });
    }

    private sealed class MarkupFileEqualityComparer : IEqualityComparer<MarkupFile>
    {
        public static readonly MarkupFileEqualityComparer Default = new();
        private MarkupFileEqualityComparer()
        {
        }

        public bool Equals(MarkupFile x, MarkupFile y) => x.Equals(y);

        public int GetHashCode(MarkupFile obj) => obj.GetHashCode();

    }

    private readonly struct MarkupFile : IEquatable<MarkupFile>
    {
        [ThreadStatic]
        private static SHA256? _sha256;
        private const int HashSizeInBytes = 32;

        public readonly string FilePath;
        private readonly byte[]? _fileHash;

        [Obsolete("Don't use default constructor.", true)]
        public MarkupFile() => throw new NotSupportedException("Don't use default constructor.");

        public MarkupFile(string filePath, byte[] hash)
        {
            if(hash.Length != HashSizeInBytes) {
                throw new ArgumentException("Invalid hash size");
            }
            FilePath = filePath;
            _fileHash = hash;
        }

        public static byte[] ComputeFileHash(string filePath)
        {
            _sha256 ??= SHA256.Create();
            using var stream = File.OpenRead(filePath);
            return _sha256.ComputeHash(stream);
        }

        public override bool Equals(object? obj) => obj is MarkupFile file && Equals(file);

        public bool Equals(MarkupFile other)
        {
            return FilePath == other.FilePath &&
                   _fileHash.AsSpan().SequenceEqual(other._fileHash);
        }

        public override int GetHashCode()
        {
            var fileHash = _fileHash;
            ulong sum = 0;
            if(fileHash != null) {
                ref var h0 = ref Unsafe.As<byte, ulong>(ref MemoryMarshal.GetReference(fileHash.AsSpan()));
                sum = h0 + Unsafe.Add(ref h0, 1) +
                      Unsafe.Add(ref h0, 2) + Unsafe.Add(ref h0, 3);
            }
            int hashCode = 1636216199;
            hashCode = hashCode * -1521134295 + (FilePath?.GetHashCode() ?? 0);
            hashCode = hashCode * -1521134295 + (int)sum;
            return hashCode;
        }
    }

    private sealed class MarkupTranslationResult : IMarkupTranslationResultHolder, IDiagnosticAccumulator
    {
        private string? _outputName;
        private SourceText? _sourceText;
        private List<Diagnostic>? _diagnosticList;

        public MarkupTranslationResult()
        {
        }

        public void SetOutputName(string outputName)
        {
            _outputName = outputName;
        }

        public void SetResult(SourceText sourceText)
        {
            _sourceText = sourceText;
        }

        public bool TryGetResult([MaybeNullWhen(false)] out string outputName, [MaybeNullWhen(false)] out SourceText sourceText)
        {
            outputName = _outputName;
            sourceText = _sourceText;
            return sourceText != null && outputName != null;
        }

        public void AddDiagnostic(Diagnostic diagnostic)
        {
            _diagnosticList ??= new List<Diagnostic>();
            _diagnosticList.Add(diagnostic);
        }

        public void ReportDiagnosticTo(SourceProductionContext context)
        {
            if(_diagnosticList == null) { return; }
            foreach(var d in _diagnosticList) {
                context.ReportDiagnostic(d);
            }
        }

        void IDiagnosticAccumulator.AddDiagnostic(object diagnostic)
        {
            if(diagnostic is Diagnostic d) {
                AddDiagnostic(d);
            }
        }
    }
}

public interface IMarkupTranslationResultHolder
{
    void AddDiagnostic(Diagnostic diagnostic);
    void SetResult(SourceText sourceText);
}
