using System;
using System.Collections.Generic;
using System.IO;
using FontStashSharp;

namespace Dreambit;

public class SpriteFontBaseLoader : AssetLoaderBase<SpriteFontBaseLoader>
{
    private readonly Dictionary<string, FontSystem> _byName = new(32);

    private readonly List<FontSystem> _fontSystems = new(32);
    public override string Extension { get; } = ".ttf";
    public override bool AddToDisposableList { get; } = true;
    public override Type TargetType { get; } = typeof(SpriteFontBase);


    public override object Load(string assetName, string pakName, bool usePak, string contentDirectory)
    {
        Logger.Warn("Font not loaded, please use Resources.LoadFont() instead");
        return null;
    }

    public SpriteFontBase LoadFont(string assetName, string contentPath, float fontSize)
    {
        FontSystemDefaults.FontResolutionFactor = 6.0f;
        FontSystemDefaults.KernelWidth = 2;
        FontSystemDefaults.KernelHeight = 2;

        if (!_byName.TryGetValue(assetName, out var value))
        {
            var fontSystem = new FontSystem();
            var ttf = File.ReadAllBytes(Path.Combine(contentPath, assetName + Extension));
            fontSystem.AddFont(ttf);
            value = fontSystem;
            _byName.Add(assetName, value);
        }

        var font = value.GetFont(fontSize);

        return font;
    }
}