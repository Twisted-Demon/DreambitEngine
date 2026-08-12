using System;
using FontStashSharp;

using System.IO;

namespace Dreambit;

public class SpriteFontBaseLoader : AssetLoaderBase<SpriteFontBaseLoader>
{
    public override string Extension { get; } = ".ttfb";
    public override bool AddToDisposableList { get; } = false;
    public override Type TargetType { get; } = typeof(SpriteFontBase);


    public override object Load(string assetName, string pakName, bool usePak, string contentDirectory)
    {
        Logger.Warn("Font not loaded, please use Resources.LoadFont() instead");
        return null;
    }

    public SpriteFontBase LoadFont(string assetName, string contentPath, float fontSize)
    {
        var normalized = assetName.Replace('\\', '/').Trim().TrimEnd('/');
        if (!normalized.Contains('/'))
            normalized = "Fonts/" + normalized;
        var fontAsset = Resources.LoadAsset<FontAsset>(normalized)
                        ?? throw new FileNotFoundException($"Font asset '{normalized}' was not found.");
        return fontAsset.GetFont(fontSize);
    }
}
