using System;
using System.IO;
using Microsoft.Xna.Framework.Content.Pipeline;
using Newtonsoft.Json;

namespace PixelariaEngine;

[ContentImporter(".animation", DisplayName = "Animation Importer - Pixelaria Engine",
    DefaultProcessor = nameof(AnimationProcessor))]
public class AnimationImporter : ContentImporter<SpriteSheetAnimation>
{
    public override SpriteSheetAnimation Import(string filename, ContentImporterContext context)
    {
        try
        {
            var json = File.ReadAllText(filename);

            var spriteFrames = JsonConvert.DeserializeObject<SpriteSheetAnimation>(json);

            return spriteFrames;
        }
        catch (Exception ex)
        {
            context.Logger.LogMessage("Error: {0}", ex.Message);

            return null;
        }
    }
}