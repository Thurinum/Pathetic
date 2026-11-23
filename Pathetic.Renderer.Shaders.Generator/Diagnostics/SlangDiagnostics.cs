using Prowl.Slang;

namespace Pathetic.Renderer.Shaders.Generator.Diagnostics;

internal static class SlangDiagnostics
{
    public static void ThrowIfErrors(DiagnosticInfo diagnostics)
    {
        List<string> errors = [.. diagnostics.GetDiagnostics()
            .Where(static diagnostic => diagnostic.Severity >= Severity.Error)
            .Select(static diagnostic => DiagnosticFormatter.Format(diagnostic))];

        if (errors.Count == 0)
            errors.AddRange(DiagnosticFormatter.ReadErrors(diagnostics.Message));

        if (errors.Count > 0)
            throw new SlangDiagnosticException(string.Join(Environment.NewLine, errors));
    }
}
