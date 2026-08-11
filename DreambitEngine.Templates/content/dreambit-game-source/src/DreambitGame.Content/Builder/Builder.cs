using Microsoft.Xna.Framework.Content.Pipeline;
using MonoGame.Framework.Content.Pipeline.Builder;

var projectDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", ".."));
var builder = new GameContentBuilder();
if (args.Length > 0)
    builder.Run(args);
else
    builder.Run(new ContentBuilderParams
    {
        Mode = ContentBuilderMode.Builder,
        WorkingDirectory = projectDirectory,
        SourceDirectory = "Assets",
        Platform = TargetPlatform.DesktopGL
    });
return builder.FailedToBuild > 0 ? -1 : 0;

public sealed class GameContentBuilder : ContentBuilder
{
    public override IContentCollection GetContentCollection()
    {
        var content = new ContentCollection();
        content.Include<WildcardRule>("*.fx");
        content.IncludeCopy<WildcardRule>("*.ttf");
        content.Exclude<WildcardRule>("*.xnb");
        return content;
    }
}
