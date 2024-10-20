using System.Linq;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace PixelariaEngine;

public class SpriteSheetAnimation : PixelariaAsset
{
    [JsonProperty("frames")] private AnimationFrame[] _frames;

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

    [JsonIgnore] public AnimationFrame[] Frames => _frames;

    [JsonProperty("frame_rate")] public int FrameRate { get; set; }

    [JsonProperty("sprite_sheet_path")] public string SpriteSheetPath { get; set; } = string.Empty;

    [JsonProperty("one_shot", NullValueHandling = NullValueHandling.Ignore)]
    public bool OneShot { get; set; }

    public AnimationFrame this[int key]
    {
        get => _frames[key];
        set => _frames[key] = value;
    }

    [JsonIgnore] public int FrameCount => _frames.Length;

    public bool TryGetFrame(int key, out AnimationFrame frame)
    {
        frame = _frames.ElementAtOrDefault(key);

        return frame != null;
    }
}

public class AnimationFrame
{
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

    [JsonProperty("frame_index")] public int FrameIndex { get; set; }

    [JsonProperty("pivot")] public Vector2 Pivot { get; set; }
}

public class AnimationEvent
{
    [JsonProperty("args", NullValueHandling = NullValueHandling.Ignore)]
    public string[] Args = [];

    [JsonProperty("name")] public string Name;

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