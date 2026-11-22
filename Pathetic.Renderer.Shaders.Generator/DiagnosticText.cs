using System.Text.RegularExpressions;

namespace Pathetic.Renderer.Shaders.Generator;

internal static class DiagnosticText
{
    private static readonly Regex Header = new(
        @"^(?:fatal\s+)?(?<severity>error|warning|note)(?:\[(?<code>[^\]]+)\])?:\s*(?<text>.*)$",
        RegexOptions.Compiled);

    private static readonly Regex Arrow = new(
        @"^\s*-->\s*(?<file>.+):(?<line>\d+):(?<column>\d+)\s*$",
        RegexOptions.Compiled);

    private static readonly Regex ErrorLine = new(
        @"^(?:fatal\s+)?error(?:\[[^\]]*\])?:",
        RegexOptions.Compiled | RegexOptions.Multiline);

    public static bool HasErrors(string? message) =>
        !string.IsNullOrEmpty(message) && ErrorLine.IsMatch(message);

    public static string Format(string? message)
    {
        if (string.IsNullOrEmpty(message))
            return string.Empty;

        string[] lines = message.ReplaceLineEndings("\n").Split('\n');
        List<string> output = [];
        int skipped = -1;

        for (int i = 0; i < lines.Length; i++)
        {
            if (i == skipped)
                continue;

            Match header = Header.Match(lines[i]);

            if (!header.Success)
            {
                output.Add(lines[i]);
                continue;
            }

            string severity = header.Groups["severity"].Value;
            string code = header.Groups["code"].Value;
            string text = header.Groups["text"].Value;
            Match arrow = i + 1 < lines.Length ? Arrow.Match(lines[i + 1]) : Match.Empty;

            if (!arrow.Success)
            {
                output.Add(Decorate(severity, code, text));
                continue;
            }

            skipped = i + 1;
            string location = $"{arrow.Groups["file"].Value}({arrow.Groups["line"].Value},{arrow.Groups["column"].Value})";
            output.Add($"{location}: {Decorate(severity, code, text)}");
        }

        return string.Join(Environment.NewLine, output);
    }

    private static string Decorate(string severity, string code, string text) =>
        code.Length > 0 ? $"{severity} {code}: {text}" : $"{severity}: {text}";
}
