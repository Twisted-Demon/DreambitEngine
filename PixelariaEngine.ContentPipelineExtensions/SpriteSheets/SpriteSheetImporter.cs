using System;
using System.IO;
using Microsoft.Xna.Framework.Content.Pipeline;
using Newtonsoft.Json;

namespace PixelariaEngine;

[ContentImporter(".spritesheet", DisplayName = "SpriteSheet Importer - Pixelaria Engine", DefaultProcessor = nameof(SpriteSheetProcessor))]
public class SpriteSheetImporter : ContentImporter<SpriteSheet>
{
    public override SpriteSheet Import(string filename, ContentImporterContext context)
    {
        try
        {
            var json = File.ReadAllText(filename);

            var spriteSheet = JsonConvert.DeserializeObject<SpriteSheet>(json);

            return spriteSheet;
        }
        catch (Exception e)
        {
            context.Logger.LogMessage("Error: {0}", e.Message);
            throw;
        }
    }
}
