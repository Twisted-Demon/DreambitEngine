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

    public static bool TryCompare(string? left, string? right, out int comparison)
    {
        comparison = 0;
        if (!IsValid(left) || !IsValid(right))
            return false;

        var leftParts = Split(left!);
        var rightParts = Split(right!);
        for (var index = 0; index < 4; index++)
        {
            comparison = leftParts.Numeric[index].CompareTo(rightParts.Numeric[index]);
            if (comparison != 0)
                return true;
        }

        if (leftParts.PreRelease is null)
            comparison = rightParts.PreRelease is null ? 0 : 1;
        else if (rightParts.PreRelease is null)
            comparison = -1;
        else
            comparison = ComparePreRelease(leftParts.PreRelease, rightParts.PreRelease);
        return true;
    }

    private static (uint[] Numeric, string? PreRelease) Split(string version)
    {
        var withoutBuildMetadata = version.Split('+', 2)[0];
        var versionAndPreRelease = withoutBuildMetadata.Split('-', 2);
        var numeric = new uint[4];
        var numericParts = versionAndPreRelease[0].Split('.');
        for (var index = 0; index < numericParts.Length; index++)
            numeric[index] = uint.Parse(numericParts[index]);
        return (
            numeric,
            versionAndPreRelease.Length == 2 ? versionAndPreRelease[1] : null);
    }

    private static int ComparePreRelease(string left, string right)
    {
        var leftLabels = left.Split('.');
        var rightLabels = right.Split('.');
        for (var index = 0; index < Math.Min(leftLabels.Length, rightLabels.Length); index++)
        {
            var leftIsNumber = uint.TryParse(leftLabels[index], out var leftNumber);
            var rightIsNumber = uint.TryParse(rightLabels[index], out var rightNumber);
            int comparison;
            if (leftIsNumber && rightIsNumber)
                comparison = leftNumber.CompareTo(rightNumber);
            else if (leftIsNumber)
                comparison = -1;
            else if (rightIsNumber)
                comparison = 1;
            else
                comparison = string.CompareOrdinal(leftLabels[index], rightLabels[index]);

            if (comparison != 0)
                return comparison;
        }

        return leftLabels.Length.CompareTo(rightLabels.Length);
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
