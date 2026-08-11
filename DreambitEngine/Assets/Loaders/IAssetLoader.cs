using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace Dreambit;

public interface IAssetLoader
{
    string Extension { get; }
    bool AddToDisposableList { get; }
    Type TargetType { get; }
    object Load(string assetName, string pakName, bool usePak, string contentDirectory);
}

public abstract class AssetLoaderBase : IAssetLoader
{
    public abstract string Extension { get; }
    public abstract bool AddToDisposableList { get; }
    public abstract Type TargetType { get; }
    public abstract object Load(string assetName, string pakName, bool usePak, string contentDirectory);

    protected static Stream GetStream(string assetName, string pakName, bool usePak, string contentDirectory)
    {
        return Resources.OpenAssetStream(assetName, pakName, usePak, contentDirectory);
    }

    protected string GetPath(string assetName)
    {
        return assetName + Extension;
    }

    /// <summary>
    /// Opens a baked JSON document while accepting the logical name, source .json name, baked
    /// .jsonb name, or a short name without its semantic suffix.
    /// </summary>
    protected Stream GetDocumentStream(
        string assetName,
        string semanticSuffix,
        string pakName,
        bool usePak,
        string contentDirectory,
        out string resolvedAssetName)
    {
        var normalized = assetName.Replace('\\', '/').Trim();
        var candidates = new List<string>();
        if (normalized.EndsWith(Extension, StringComparison.OrdinalIgnoreCase))
        {
            candidates.Add(normalized);
        }
        else if (normalized.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            candidates.Add(normalized[..^".json".Length] + Extension);
        }
        else
        {
            candidates.Add(normalized + Extension);
            if (!normalized.EndsWith(semanticSuffix, StringComparison.OrdinalIgnoreCase))
                candidates.Add(normalized + semanticSuffix + Extension);
        }

        foreach (var candidate in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                var stream = GetStream(candidate, pakName, usePak, contentDirectory);
                resolvedAssetName = candidate[..^Extension.Length];
                return stream;
            }
            catch (FileNotFoundException)
            {
            }
            catch (DirectoryNotFoundException)
            {
            }
        }

        throw new FileNotFoundException(
            $"Asset '{assetName}' was not found. Tried: {string.Join(", ", candidates)}");
    }
}

public abstract class AssetLoaderBase<T> : AssetLoaderBase where T : class
{
    protected Logger<T> Logger = new();
}
