using System;
using Newtonsoft.Json.Linq;

namespace Dreambit;

/// <summary>
/// JSON representation of an ID-based Dreambit asset reference. A path is retained only as a
/// diagnostic and backward-compatible fallback when no registry is installed.
/// </summary>
public static class DreambitAssetReferenceToken
{
    public const string IdPropertyName = "$dreambitAsset";
    public const string PathPropertyName = "path";

    public static JObject Create(AssetId assetId, string assetName = null)
    {
        if (assetId.IsEmpty)
            throw new ArgumentException("An asset reference ID cannot be empty.", nameof(assetId));

        var token = new JObject
        {
            [IdPropertyName] = assetId.ToString()
        };

        if (!string.IsNullOrWhiteSpace(assetName))
            token[PathPropertyName] = assetName;

        return token;
    }

    public static bool TryRead(JToken token, out AssetId assetId, out string assetName)
    {
        assetId = AssetId.Empty;
        assetName = null;

        if (token is not JObject jsonObject ||
            !AssetId.TryParse(jsonObject.Value<string>(IdPropertyName), out assetId))
        {
            return false;
        }

        assetName = jsonObject.Value<string>(PathPropertyName);
        return true;
    }
}
