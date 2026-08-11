using System.Security.Cryptography;
using System.Text;

namespace Dreambit.Editor.Infrastructure;

internal sealed class EditorPaths
{
    private const string HubScopeName = "hub";

    private EditorPaths(string settingsRoot, string workspaceScope)
    {
        SettingsRoot = settingsRoot;
        SdkRootDirectory = Path.Combine(settingsRoot, "sdks");
        GlobalStatePath = Path.Combine(settingsRoot, "editor-state.json");
        WorkspaceDirectory = Path.Combine(settingsRoot, "workspaces", workspaceScope);
        WorkspaceStatePath = Path.Combine(WorkspaceDirectory, "workspace-state.json");
        ImGuiLayoutPath = Path.Combine(WorkspaceDirectory, "layout.ini");
        ProjectLockPath = CreateProjectLockPathFromScope(workspaceScope);
    }

    public string SettingsRoot { get; }
    public string SdkRootDirectory { get; }
    public string GlobalStatePath { get; }
    public string WorkspaceDirectory { get; }
    public string WorkspaceStatePath { get; }
    public string ImGuiLayoutPath { get; }
    public string ProjectLockPath { get; }

    public static EditorPaths Create(EditorLaunchOptions options)
    {
        var settingsRoot = string.IsNullOrWhiteSpace(options.SettingsDirectory)
            ? GetDefaultSettingsRoot()
            : Path.GetFullPath(options.SettingsDirectory);

        var workspaceScope = HubScopeName;
        if (!string.IsNullOrWhiteSpace(options.ProjectPath))
        {
            try
            {
                workspaceScope = CreateProjectScope(options.ProjectPath);
            }
            catch (Exception exception) when (
                exception is ArgumentException or NotSupportedException or PathTooLongException)
            {
                workspaceScope = HubScopeName;
            }
        }

        return new EditorPaths(settingsRoot, workspaceScope);
    }

    internal static string CreateProjectScope(string projectPath)
    {
        var normalizedPath = Path.GetFullPath(projectPath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        if (OperatingSystem.IsWindows())
            normalizedPath = normalizedPath.ToUpperInvariant();

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalizedPath));
        return Convert.ToHexString(hash.AsSpan(0, 10)).ToLowerInvariant();
    }

    internal static string CreateProjectLockPath(string projectPath) =>
        CreateProjectLockPathFromScope(CreateProjectScope(projectPath));

    private static string CreateProjectLockPathFromScope(string workspaceScope) =>
        Path.Combine(
            Path.GetTempPath(),
            "Dreambit",
            "Editor",
            "locks",
            $"{workspaceScope}.lock");

    private static string GetDefaultSettingsRoot()
    {
        var localApplicationData = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData,
            Environment.SpecialFolderOption.Create);

        if (string.IsNullOrWhiteSpace(localApplicationData))
            localApplicationData = AppContext.BaseDirectory;

        return Path.Combine(localApplicationData, "Dreambit", "Editor");
    }
}
