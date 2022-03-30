#nullable enable

namespace Elffy.Markup;

public interface IDiagnosticAccumulator
{
    void AddDiagnostic(object diagnostic);
}
