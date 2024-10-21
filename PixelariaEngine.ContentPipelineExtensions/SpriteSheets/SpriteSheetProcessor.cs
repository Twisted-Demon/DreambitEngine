using Microsoft.Xna.Framework.Content.Pipeline;

namespace Dreambit;

[ContentProcessor(DisplayName = "Sprite Sheet Processor - Dreambit Engine")]
public class SpriteSheetProcessor : ContentProcessor<SpriteSheet, SpriteSheet>
{
    public override SpriteSheet Process(SpriteSheet input, ContentProcessorContext context)
    {
        return input;
    }
}