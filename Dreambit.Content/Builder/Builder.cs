using Microsoft.Xna.Framework.Content.Pipeline;
using MonoGame.Framework.Content.Pipeline.Builder;

if (args.Length == 0)
{
    Console.Error.WriteLine(
        "No content platform was supplied. " +
        "Build a host project instead.");

    return -1;
}

var builder = new Builder();
builder.Run(args);

return builder.FailedToBuild > 0 ? -1 : 0;

public sealed class Builder : ContentBuilder
{
    public override IContentCollection GetContentCollection()
    {
        var content = new ContentCollection();

        content.Include("Effects/ColorCorrection.fx");
        content.Include("Effects/ForwardDiffuse.fx");
        content.Include("Effects/ForwardLighting2D.fx");
        content.Include("Effects/PointLight2D.fx");
        content.Include("Effects/Tint.fx");
        content.Include("Effects/WorldRing.fx");
        content.IncludeCopy<WildcardRule>("*.ttf");
        content.Exclude<WildcardRule>("*.xnb");

        return content;
    }
}
