using System;
using System.IO;
using System.Linq;

namespace Dreambit;

public class SpriteSheetAnimationLoader : AssetLoaderBase
{
    public override string Extension { get; } = ".jsonb";
    public override bool AddToDisposableList { get; } = true;
    public override Type TargetType { get; } = typeof(SpriteSheetAnimation);

    public override object Load(string assetName, string pakName, bool usePak, string contentDirectory)
    {
        using var stream = GetStream(GetPath(assetName), pakName, usePak, contentDirectory);
        var animation = JsnbLoader.Deserialize<SpriteSheetAnimation>(stream);
        animation.AssetName = assetName;

        var errors = animation.GetValidationErrors();
        if (errors.Count > 0)
            throw new InvalidDataException(
                $"Sprite animation '{assetName}' is invalid:{Environment.NewLine}" +
                string.Join(Environment.NewLine, errors.Select(error => $"- {error}")));

        return animation;
    }
}
