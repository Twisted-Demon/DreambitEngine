using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace DreambitEngine.AssetBaker.Pipeline;

internal static class BuiltInContentSource
{
    private const string Marker = ".BuiltInContent.";
    private const string VersionFileName = "builtins.version";
    private static readonly Lazy<string> Root = new(Materialize);
    private static readonly Lazy<string> ContentVersion = new(ComputeContentVersion);

    public static string DirectoryPath => Root.Value;

    public static bool IsCurrent(string cacheDirectory)
    {
        if (string.IsNullOrWhiteSpace(cacheDirectory))
            return false;
        try
        {
            var versionPath = Path.Combine(Path.GetFullPath(cacheDirectory), VersionFileName);
            return File.Exists(versionPath) &&
                   string.Equals(
                       File.ReadAllText(versionPath).Trim(),
                       ContentVersion.Value,
                       StringComparison.Ordinal);
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    public static void MarkCurrent(string cacheDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cacheDirectory);
        var fullPath = Path.GetFullPath(cacheDirectory);
        Directory.CreateDirectory(fullPath);
        File.WriteAllText(Path.Combine(fullPath, VersionFileName), ContentVersion.Value);
    }

    private static string Materialize()
    {
        var assembly = typeof(BuiltInContentSource).Assembly;
        var resources = assembly.GetManifestResourceNames()
            .Where(name => name.Contains(Marker, StringComparison.Ordinal))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        if (resources.Length == 0)
            throw new InvalidOperationException("Dreambit built-in content was not embedded in AssetBaker.");

        var identity = Convert.ToHexString(SHA256.HashData(
                Encoding.UTF8.GetBytes(assembly.ManifestModule.ModuleVersionId.ToString("N"))))
            .ToLowerInvariant()[..16];
        var root = Path.Combine(Path.GetTempPath(), "DreambitEngine.AssetBaker", "builtin-" + identity);
        Directory.CreateDirectory(root);
        foreach (var resourceName in resources)
        {
            var tail = resourceName[(resourceName.IndexOf(Marker, StringComparison.Ordinal) + Marker.Length)..];
            var firstDot = tail.IndexOf('.');
            var lastDot = tail.LastIndexOf('.');
            if (firstDot <= 0 || lastDot <= firstDot)
                continue;
            var folder = tail[..firstDot];
            var fileName = tail[(firstDot + 1)..lastDot] + tail[lastDot..];
            var path = Path.Combine(root, folder, fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            using var input = assembly.GetManifestResourceStream(resourceName)
                              ?? throw new InvalidOperationException($"Could not read embedded resource '{resourceName}'.");
            using var memory = new MemoryStream();
            input.CopyTo(memory);
            var bytes = memory.ToArray();
            if (File.Exists(path) && File.ReadAllBytes(path).AsSpan().SequenceEqual(bytes))
                continue;

            // The Editor and a game build may start the baker at the same time.
            // Never expose a partially extracted shader/font or require an exclusive
            // handle to the final path while the other baker is compiling it.
            var temporaryPath = path + $".{Environment.ProcessId}.{Guid.NewGuid():N}.tmp";
            try
            {
                File.WriteAllBytes(temporaryPath, bytes);
                try
                {
                    File.Move(temporaryPath, path, true);
                }
                catch (IOException) when (
                    File.Exists(path) && File.ReadAllBytes(path).AsSpan().SequenceEqual(bytes))
                {
                }
            }
            finally
            {
                try
                {
                    File.Delete(temporaryPath);
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
            }
        }
        return root;
    }

    private static string ComputeContentVersion()
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var path in Directory.EnumerateFiles(DirectoryPath, "*", SearchOption.AllDirectories)
                     .OrderBy(path => Path.GetRelativePath(DirectoryPath, path), StringComparer.Ordinal))
        {
            var relativePath = Path.GetRelativePath(DirectoryPath, path).Replace('\\', '/');
            hash.AppendData(Encoding.UTF8.GetBytes(relativePath));
            hash.AppendData([0]);
            hash.AppendData(File.ReadAllBytes(path));
            hash.AppendData([0]);
        }
        return Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();
    }
}
