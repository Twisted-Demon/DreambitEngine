using Microsoft.Xna.Framework.Content;

namespace PixelariaEngine;

public class SpriteSheetAnimationReader : ContentTypeReader<SpriteSheetAnimation>
{
    protected override SpriteSheetAnimation Read(ContentReader input, SpriteSheetAnimation existingInstance)
    {
        //get the number of frames and frame rate
        var spriteSheetPath = input.ReadString();
        var frameRate = input.ReadInt32();
        var framesCount = input.ReadInt32();
        var oneShot = input.ReadBoolean();

        var spriteSheetAnimation = new SpriteSheetAnimation(framesCount)
        {
            AssetName = input.AssetName,
            FrameRate = frameRate,
            SpriteSheetPath = spriteSheetPath,
            OneShot = oneShot,
        };

        for (var i = 0; i < framesCount; i++)
        {
            var frame = new AnimationFrame();

            //read the frame index
            var index = input.ReadInt32();
            frame.FrameIndex = index;

            //read the pivot
            var pivot = input.ReadVector2();
            frame.Pivot = pivot;

            //set the frame
            spriteSheetAnimation[i] = frame;

            //move on if we don't have an animation event
            if (!input.ReadBoolean())
                continue;

            //get the name of the animation event
            var animationEvent = new AnimationEvent
            {
                Name = input.ReadString()
            };

            //get the number of arguments
            var argCount = input.ReadInt32();

            //initialize the arguments
            animationEvent.Args = new string[argCount];

            //read all the arguments
            for (var argIndex = 0; argIndex < argCount; argIndex++) animationEvent.Args[argIndex] = input.ReadString();

            //set the animation event for the frame
            frame.AnimationEvent = animationEvent;
        }

        return spriteSheetAnimation;
    }
}