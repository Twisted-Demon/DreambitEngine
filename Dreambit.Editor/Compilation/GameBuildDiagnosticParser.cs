using System.Text.RegularExpressions;

namespace Dreambit.Editor.Compilation;

internal static partial class GameBuildDiagnosticParser
{
    [GeneratedRegex(
        "^(?<file>.+?)\\((?<line>\\d+),(?<column>\\d+)\\):\\s*(?<severity>error|warning)\\s+(?<code>[A-Za-z]+\\d+):\\s*(?<message>.*?)(?:\\s+\\[[^]]+\\])?$",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex FileDiagnosticRegex();

    [GeneratedRegex(
        "^(?<severity>error|warning)\\s+(?<code>[A-Za-z]+\\d+):\\s*(?<message>.*?)(?:\\s+\\[[^]]+\\])?$",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex GeneralDiagnosticRegex();

    public static IReadOnlyList<GameBuildDiagnostic> Parse(IEnumerable<string> output)
    {
        var diagnostics = new List<GameBuildDiagnostic>();
        foreach (var rawLine in output)
        {
            var line = rawLine.Trim();
            var match = FileDiagnosticRegex().Match(line);
            if (match.Success)
            {
                diagnostics.Add(new GameBuildDiagnostic(
                    ParseSeverity(match.Groups["severity"].Value),
                    match.Groups["code"].Value,
                    match.Groups["message"].Value.Trim(),
                    match.Groups["file"].Value,
                    int.Parse(match.Groups["line"].Value),
                    int.Parse(match.Groups["column"].Value),
                    rawLine));
                continue;
            }

            match = GeneralDiagnosticRegex().Match(line);
            if (match.Success)
            {
                diagnostics.Add(new GameBuildDiagnostic(
                    ParseSeverity(match.Groups["severity"].Value),
                    match.Groups["code"].Value,
                    match.Groups["message"].Value.Trim(),
                    Raw: rawLine));
            }
        }

        return diagnostics
            .DistinctBy(diagnostic => new
            {
                diagnostic.Severity,
                diagnostic.Code,
                diagnostic.Message,
                diagnostic.File,
                diagnostic.Line,
                diagnostic.Column
            })
            .ToArray();
    }

    private static GameBuildDiagnosticSeverity ParseSeverity(string severity) =>
        severity.Equals("error", StringComparison.OrdinalIgnoreCase)
            ? GameBuildDiagnosticSeverity.Error
            : GameBuildDiagnosticSeverity.Warning;
}
