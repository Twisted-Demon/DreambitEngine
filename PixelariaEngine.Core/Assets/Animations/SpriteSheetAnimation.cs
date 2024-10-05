using System;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace PixelariaEngine;

public class SpriteSheetAnimation : PixelariaAsset
{
    [JsonProperty("frames")] 
    private AnimationFrame[] _frames;
    
    [JsonProperty("frame_rate")] public int FrameRate = 0;
    
    public AnimationFrame this[int key]
    {
        get => _frames[key];
        set => _frames[key] = value;
    }

    [JsonIgnore]
    public int FrameCount => _frames.Length;

    internal SpriteSheetAnimation(AnimationFrame[] frames)
    {
        _frames = frames;
    }

    internal SpriteSheetAnimation(int frameCount)
    {
        _frames = new AnimationFrame[frameCount];
    }

    public SpriteSheetAnimation()
    {
        _frames = [];
    }
}

public class AnimationFrame
{
    [JsonProperty("frame_index")]
    public int FrameIndex;
    [JsonProperty("animation_event", NullValueHandling = NullValueHandling.Ignore)]
    public AnimationEvent AnimationEvent;

    public AnimationFrame(int frameIndex, AnimationEvent animationEvent = null)
    {
        FrameIndex = frameIndex;
        AnimationEvent = animationEvent;
    }

    public AnimationFrame()
    {
        
    }
}

public class AnimationEvent
{
    [JsonProperty("name")]
    public string Name;
    [JsonProperty("args")]
    public string[] Args = [];

    public AnimationEvent(string name, params string[] args)
    {
        Name = name;
        Args = args;
    }

    public AnimationEvent(string name, string arg)
    {
        Name = name;
        Args = new[] { arg };
    }
    
    public AnimationEvent()
    {
        
    }
}

