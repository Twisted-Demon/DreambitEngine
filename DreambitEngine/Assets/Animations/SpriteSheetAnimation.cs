using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace Dreambit;

public class SpriteSheetAnimation : DreambitAsset
{
    private AnimationFrame[] Frames { get; set; } = [];

    [JsonProperty("frame_overrides")] private List<AnimationFrame> FrameOverrides { get; set; } = [];

    [JsonProperty("frame_rate")] public int FrameRate { get; set; }

    [JsonProperty("sprite_sheet_path")] public string SpriteSheetPath { get; set; } = string.Empty;

    [JsonProperty("one_shot", NullValueHandling = NullValueHandling.Ignore)]
    public bool OneShot { get; set; }

    [JsonIgnore] public int FrameCount => Frames.Length;

    [JsonProperty("index_start")] public int IndexStart { get; set; }
    [JsonProperty("index_end")] public int IndexEnd { get; set; }
    [JsonProperty("pivot")] public Vector2 Pivot { get; set; }

    public AnimationFrame this[int key]
    {
        get => Frames[key];
        set => Frames[key] = value;
    }

    public bool TryGetFrame(int key, out AnimationFrame frame)
    {
        frame = Frames.ElementAtOrDefault(key);

        return frame != null;
    }

    public void Initialize()
    {
        var totalFrames = IndexEnd - IndexStart + 1;
        Frames = new AnimationFrame[totalFrames];

        for (var i = IndexStart; i < IndexEnd + 1; i++)
            Frames[i] = new AnimationFrame
            {
                FrameIndex = i,
                Pivot = Pivot
            };

        foreach (var frameOverride in FrameOverrides)
            if (TryGetFrame(frameOverride.FrameIndex, out var frame))
            {
                frame.Pivot = frameOverride.Pivot;
                frame.AnimationEvent = frameOverride.AnimationEvent;
            }
    }
}

public class AnimationFrame
{
    [JsonProperty("event", NullValueHandling = NullValueHandling.Ignore)]
    public AnimationEvent AnimationEvent;

    [JsonProperty("frame_index")] public int FrameIndex { get; set; }

    [JsonProperty("pivot")] public Vector2 Pivot { get; set; }
}

public class AnimationEvent
{
    [JsonProperty("args", NullValueHandling = NullValueHandling.Ignore)]
    public readonly Dictionary<string, string> Args = [];

    [JsonProperty("name")] public readonly string Name;
}