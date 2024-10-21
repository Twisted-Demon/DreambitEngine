using Microsoft.Xna.Framework.Content.Pipeline;

namespace Dreambit;

[ContentProcessor(DisplayName = "Animation Processor - Dreambit Engine")]
public class AnimationProcessor : ContentProcessor<SpriteSheetAnimation, SpriteSheetAnimation>
{
    public override SpriteSheetAnimation Process(SpriteSheetAnimation input, ContentProcessorContext context)
    {
        return input;
    }
}