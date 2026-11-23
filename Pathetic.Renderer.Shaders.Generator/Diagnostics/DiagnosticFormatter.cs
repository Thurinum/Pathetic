using Prowl.Slang;

namespace Pathetic.Renderer.Shaders.Generator.Diagnostics;

internal static class DiagnosticFormatter
{
    public static string Format(Diagnostic diagnostic)
    {
        string severity = diagnostic.Severity == Severity.Fatal ? "fatal error" : "error";
        string code = diagnostic.ErrorCode >= 0 ? $" {diagnostic.ErrorCode}" : string.Empty;
        string origin = diagnostic.FilePath is { Length: > 0 } path ? $"{path}({diagnostic.LineNumber}): " : string.Empty;

        return $"{origin}{severity}{code}: {diagnostic.Message}";
    }

    public static string Format(string? message)
    {
        string[] errors = [.. ReadErrors(message)];

        return errors.Length > 0
            ? string.Join(Environment.NewLine, errors)
            : message ?? string.Empty;
    }

    public static IReadOnlyList<string> ReadErrors(string? message)
    {
        List<string> errors = [];

        if (string.IsNullOrEmpty(message))
            return errors;

        string[] lines = message.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            if (!TryReadDiagnostic(lines[i].TrimEnd('\r'), out string diagnostic))
                continue;

            if (i + 1 < lines.Length && TryReadLocation(lines[i + 1].TrimEnd('\r'), out string location))
            {
                i++;
                diagnostic = $"{location}: {diagnostic}";
            }

            errors.Add(diagnostic);
        }

        return errors;
    }

    private static bool TryReadDiagnostic(string line, out string diagnostic)
    {
        diagnostic = string.Empty;

        int bracket = line.IndexOf('[');
        int end = line.IndexOf("]: ", StringComparison.Ordinal);

        if (bracket < 0 || end < bracket)
            return false;

        string severity = line[..bracket].TrimEnd();

        if (severity is not ("error" or "fatal error"))
            return false;

        diagnostic = $"{severity} {line[(bracket + 1)..end]}: {line[(end + 3)..]}";

        return true;
    }

    private static bool TryReadLocation(string line, out string location)
    {
        location = string.Empty;

        string trimmed = line.TrimStart();

        if (!trimmed.StartsWith("--> ", StringComparison.Ordinal))
            return false;

        string target = trimmed[4..];
        int column = target.LastIndexOf(':');

        if (column < 0)
            return false;

        int lineNumber = target.LastIndexOf(':', column - 1);

        if (lineNumber < 0)
            return false;

        location = $"{target[..lineNumber]}({target[(lineNumber + 1)..column]},{target[(column + 1)..]})";

        return true;
    }
}
