namespace Dreambit.Editor;

internal sealed record EditorLaunchOptions(
    string? ProjectPath,
    string? SettingsDirectory,
    bool SmokeTest)
{
    public static bool TryParse(
        IReadOnlyList<string> arguments,
        out EditorLaunchOptions options,
        out string? error)
    {
        string? projectPath = null;
        string? settingsDirectory = null;
        var smokeTest = false;

        for (var index = 0; index < arguments.Count; index++)
        {
            var argument = arguments[index];
            switch (argument.ToLowerInvariant())
            {
                case "--project":
                    if (!TryReadValue(arguments, ref index, argument, out projectPath, out error))
                    {
                        options = new EditorLaunchOptions(null, null, false);
                        return false;
                    }
                    break;

                case "--settings-dir":
                    if (!TryReadValue(arguments, ref index, argument, out settingsDirectory, out error))
                    {
                        options = new EditorLaunchOptions(null, null, false);
                        return false;
                    }
                    break;

                case "--smoke-test":
                    smokeTest = true;
                    break;

                default:
                    options = new EditorLaunchOptions(null, null, false);
                    error = $"Unknown argument '{argument}'.";
                    return false;
            }
        }

        options = new EditorLaunchOptions(projectPath, settingsDirectory, smokeTest);
        error = null;
        return true;
    }

    private static bool TryReadValue(
        IReadOnlyList<string> arguments,
        ref int index,
        string option,
        out string? value,
        out string? error)
    {
        if (index + 1 >= arguments.Count ||
            string.IsNullOrWhiteSpace(arguments[index + 1]) ||
            arguments[index + 1].StartsWith("--", StringComparison.Ordinal))
        {
            value = null;
            error = $"The {option} option requires a value.";
            return false;
        }

        value = arguments[++index];
        error = null;
        return true;
    }
}
