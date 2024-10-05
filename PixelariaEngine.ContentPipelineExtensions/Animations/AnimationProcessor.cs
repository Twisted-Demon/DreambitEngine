using Microsoft.Xna.Framework.Content.Pipeline;

namespace PixelariaEngine;

[ContentProcessor(DisplayName = "Animation Processor - PixelariaEngine")]
public class AnimationProcessor : ContentProcessor<SpriteSheetAnimation, SpriteSheetAnimation>
{
    public override SpriteSheetAnimation Process(SpriteSheetAnimation input, ContentProcessorContext context)
    {
        return input;
    }
}