namespace Dreambit.AssetEditor.Core;

internal sealed class AssetEditorProject
{
    public string? RootPath { get; private set; }

    public void SetRoot(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        RootPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
    }

    public bool TryCreateAssetReference(string filePath, out string reference)
    {
        reference = string.Empty;
        if (RootPath is null || string.IsNullOrWhiteSpace(filePath))
            return false;

        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(filePath);
        }
        catch
        {
            return false;
        }

        var relativePath = Path.GetRelativePath(RootPath, fullPath);
        if (Path.IsPathRooted(relativePath) ||
            relativePath.Equals("..", StringComparison.Ordinal) ||
            relativePath.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) ||
            relativePath.StartsWith(".." + Path.AltDirectorySeparatorChar, StringComparison.Ordinal))
        {
            return false;
        }

        var withoutExtension = Path.ChangeExtension(relativePath, null);
        if (string.IsNullOrWhiteSpace(withoutExtension))
            return false;

        reference = withoutExtension.Replace(Path.DirectorySeparatorChar, '/');
        return true;
    }
}
