using Assimp;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
using Newtonsoft.Json;

namespace PixelariaEngine;

[ContentTypeWriter]
public class AnimationWriter : ContentTypeWriter<SpriteSheetAnimation>
{
    public override string GetRuntimeReader(TargetPlatform targetPlatform)
    {
        var type = typeof(SpriteSheetAnimationReader);
        
        var typeName = type.FullName;
        var assemblyName = type.Assembly.GetName().Name;
        
        return $"{typeName}, {assemblyName}";
    }

    protected override void Write(ContentWriter output, SpriteSheetAnimation anim)
    {
        //serialize the sprite sheet path
        output.Write(anim.SpriteSheetPath);
        
        //serialize the framerate
        output.Write(anim.FrameRate);
        
        //serialize the number of frames first
        output.Write(anim.FrameCount);

        //loop through the number of frames and serialize them
        for (var i = 0; i < anim.FrameCount; i++)
        {
            WriteFrame(anim[i], output);
        }
    }

    private static void WriteFrame(AnimationFrame frame, ContentWriter output)
    {
        // write the frame index
        output.Write(frame.FrameIndex);
        
        //write the pivot
        output.Write(frame.Pivot);
            
        // write if we  have an animation event
        if (frame.AnimationEvent == null)
        {
            output.Write(false);
            return;
        }

        //output a boolean that we do have an animation event
        output.Write(true);

        //write the name of the event
        output.Write(frame.AnimationEvent.Name);
        
        //write the number of arguments
        output.Write(frame.AnimationEvent.Args.Length);
        
        foreach(var arg in frame.AnimationEvent.Args)
            output.Write(arg);
    }
}