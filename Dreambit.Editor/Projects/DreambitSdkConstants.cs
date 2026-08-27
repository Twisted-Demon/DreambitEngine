namespace Dreambit.Editor.Projects;

internal static class DreambitSdkConstants
{
    public const string CurrentVersion = "0.6.1";
    public const string RuntimePackageId = "DreambitEngine";
    public const string BuildPackageId = "DreambitEngine.Build";
    public const string EditorApiPackageId = "Dreambit.Editor.Abstractions";
    public const string TemplatePackageId = "DreambitEngine.Templates";
    public const string TemplateShortName = "dreambit-game";

    public static readonly string[] RequiredPackageIds =
    [
        RuntimePackageId,
        EditorApiPackageId,
        BuildPackageId,
        TemplatePackageId
    ];
}
