namespace Dreambit.Editor.Projects;

internal sealed record CreateProjectRequest(
    string Name,
    string Location,
    string GameTitle,
    string TargetRenderer,
    string SdkVersion)
{
    public bool TryValidate(out string outputPath, out string? error)
    {
        if (!IsValidProjectName(Name))
        {
            outputPath = string.Empty;
            error = "Project name must start with a letter and contain only letters, digits, '.', '-', or '_'.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(GameTitle))
        {
            outputPath = string.Empty;
            error = "Game title is required.";
            return false;
        }

        if (!string.Equals(TargetRenderer, "DesktopVK", StringComparison.Ordinal))
        {
            outputPath = string.Empty;
            error = $"Target renderer '{TargetRenderer}' is not supported.";
            return false;
        }

        if (!DreambitSdkVersion.IsValid(SdkVersion))
        {
            outputPath = string.Empty;
            error = "Dreambit SDK version must be a valid portable package version.";
            return false;
        }

        string normalizedLocation;
        try
        {
            normalizedLocation = Path.GetFullPath(Location);
        }
        catch (Exception exception) when (
            exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            outputPath = string.Empty;
            error = $"Project location is invalid. {exception.Message}";
            return false;
        }

        if (!Directory.Exists(normalizedLocation))
        {
            outputPath = string.Empty;
            error = $"Project location '{normalizedLocation}' does not exist.";
            return false;
        }

        outputPath = Path.Combine(normalizedLocation, Name);
        if (Directory.Exists(outputPath) && Directory.EnumerateFileSystemEntries(outputPath).Any())
        {
            error = $"Project directory '{outputPath}' is not empty.";
            return false;
        }

        error = null;
        return true;
    }

    private static bool IsValidProjectName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 64 || !char.IsAsciiLetter(name[0]))
            return false;

        foreach (var character in name)
        {
            if (!char.IsAsciiLetterOrDigit(character) &&
                character is not '.' and not '-' and not '_')
            {
                return false;
            }
        }

        if (name[^1] is '.' or ' ')
            return false;

        var deviceName = name.Split('.', 2)[0];
        return !deviceName.Equals("CON", StringComparison.OrdinalIgnoreCase) &&
               !deviceName.Equals("PRN", StringComparison.OrdinalIgnoreCase) &&
               !deviceName.Equals("AUX", StringComparison.OrdinalIgnoreCase) &&
               !deviceName.Equals("NUL", StringComparison.OrdinalIgnoreCase) &&
               !IsNumberedDevice(deviceName, "COM") &&
               !IsNumberedDevice(deviceName, "LPT");
    }

    private static bool IsNumberedDevice(string value, string prefix) =>
        value.Length == 4 &&
        value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
        value[3] is >= '1' and <= '9';
}
