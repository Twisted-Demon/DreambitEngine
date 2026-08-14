using System;
using FontStashSharp;
using Newtonsoft.Json;

namespace Dreambit;

/// <summary>Dreambit asset-system handle for a baked TrueType font.</summary>
[DreambitAssetType("dreambit.font", FileExtension = ".ttf")]
public sealed class FontAsset : DreambitAsset
{
    private FontSystem _fontSystem;

    internal FontAsset(byte[] data, string assetName)
    {
        Data = data;
        AssetName = assetName;
    }

    [JsonIgnore]
    public byte[] Data { get; private set; }

    public SpriteFontBase GetFont(float size)
    {
        if (size <= 0f)
            throw new ArgumentOutOfRangeException(nameof(size));
        if (_fontSystem is null)
        {
            FontSystemDefaults.FontResolutionFactor = 6.0f;
            FontSystemDefaults.KernelWidth = 2;
            FontSystemDefaults.KernelHeight = 2;
            _fontSystem = new FontSystem();
            _fontSystem.AddFont(Data);
        }
        return _fontSystem.GetFont(size);
    }

    protected override void CleanUp()
    {
        _fontSystem?.Dispose();
        _fontSystem = null;
        Data = null;
    }
}
