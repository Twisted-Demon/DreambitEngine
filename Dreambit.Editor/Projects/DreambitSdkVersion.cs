namespace Dreambit.Editor.Projects;

internal static class DreambitSdkVersion
{
    public static bool IsValid(string? version)
    {
        if (string.IsNullOrWhiteSpace(version) || version.Length > 64)
            return false;

        var buildParts = version.Split('+');
        if (buildParts.Length > 2 ||
            (buildParts.Length == 2 && !IsValidLabelSet(buildParts[1])))
        {
            return false;
        }

        var versionAndPrerelease = buildParts[0].Split('-', 2);
        if (versionAndPrerelease.Length == 2 && !IsValidLabelSet(versionAndPrerelease[1]))
            return false;

        var numericParts = versionAndPrerelease[0].Split('.');
        if (numericParts.Length is < 1 or > 4)
            return false;

        foreach (var numericPart in numericParts)
        {
            if (numericPart.Length == 0 ||
                !numericPart.All(char.IsAsciiDigit) ||
                !uint.TryParse(numericPart, out _))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsValidLabelSet(string value)
    {
        var labels = value.Split('.');
        foreach (var label in labels)
        {
            if (label.Length == 0 ||
                label.Any(static character =>
                    !char.IsAsciiLetterOrDigit(character) && character != '-'))
            {
                return false;
            }
        }

        return true;
    }
}
