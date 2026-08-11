using System;

namespace Dreambit;

/// <summary>
/// Stable identity for a source asset. Paths are locations and may change; this value does not.
/// </summary>
public readonly record struct AssetId(Guid Value)
{
    public static AssetId Empty => default;

    public bool IsEmpty => Value == Guid.Empty;

    public static AssetId New() => new(Guid.NewGuid());

    public static bool TryParse(string value, out AssetId assetId)
    {
        if (Guid.TryParse(value, out var guid) && guid != Guid.Empty)
        {
            assetId = new AssetId(guid);
            return true;
        }

        assetId = Empty;
        return false;
    }

    public override string ToString() => Value.ToString("D");
}
