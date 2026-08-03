using System;
using System.Collections.Generic;
using DreambitEngine.AssetBaker.Abstractions;

namespace DreambitEngine.AssetBaker.Core;

public sealed class AssetBakerRegistry
{
    private readonly Dictionary<AssetType, IAssetBaker> _byType = new();

    private readonly Dictionary<string, IAssetBaker> _byExtension =
        new(StringComparer.OrdinalIgnoreCase);

    public AssetBakerRegistry Register(
        AssetType type,
        IAssetBaker baker)
    {
        ArgumentNullException.ThrowIfNull(baker);

        _byType[type] = baker;

        foreach (var extension in baker.SupportedInputs)
        {
            var normalizedExtension = NormalizeExtension(extension);

            if (normalizedExtension.Length == 0)
            {
                throw new InvalidOperationException(
                    $"Baker '{baker.GetType().FullName}' registered an empty extension.");
            }

            _byExtension[normalizedExtension] = baker;
        }

        return this;
    }

    public IAssetBaker Get(AssetType type)
    {
        return _byType.TryGetValue(type, out var baker)
            ? baker
            : throw new InvalidOperationException(
                $"No baker is registered for asset type '{type}'.");
    }

    public IAssetBaker? GetByExt(string extension)
    {
        var normalizedExtension = NormalizeExtension(extension);

        return _byExtension.TryGetValue(
            normalizedExtension,
            out var baker)
            ? baker
            : null;
    }

    private static string NormalizeExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
            return string.Empty;

        extension = extension.Trim();

        return extension[0] == '.'
            ? extension
            : "." + extension;
    }
}