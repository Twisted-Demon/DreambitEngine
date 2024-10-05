using Microsoft.Xna.Framework.Content.Pipeline;

namespace PixelariaEngine;

[ContentProcessor(DisplayName = "Sprite Sheet Processor - PixelariaEngine")]
public class SpriteSheetProcessor : ContentProcessor<SpriteSheet, SpriteSheet>
{
    public override SpriteSheet Process(SpriteSheet input, ContentProcessorContext context)
    {
        return input;
    }
}