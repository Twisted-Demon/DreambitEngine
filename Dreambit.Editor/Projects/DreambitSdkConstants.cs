namespace Dreambit.Editor.Projects;

internal static class DreambitSdkConstants
{
    public const string CurrentVersion = "0.1.4";
    public const string RuntimePackageId = "DreambitEngine";
    public const string BuildPackageId = "DreambitEngine.Build";
    public const string TemplatePackageId = "DreambitEngine.Templates";
    public const string TemplateShortName = "dreambit-game";

    public static readonly string[] RequiredPackageIds =
    [
        RuntimePackageId,
        BuildPackageId,
        TemplatePackageId
    ];
}
