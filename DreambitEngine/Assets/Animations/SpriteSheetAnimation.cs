using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace Dreambit;

public class SpriteSheetAnimation : DreambitAsset
{
    [JsonProperty("frames")] private AnimationFrame[] _frames;

    [JsonProperty("frame_rate")] public int FrameRate { get; set; }

    [JsonProperty("sprite_sheet_path")] public string SpriteSheetPath { get; set; } = string.Empty;

    [JsonProperty("one_shot", NullValueHandling = NullValueHandling.Ignore)]
    public bool OneShot { get; set; }

    [JsonIgnore] public int FrameCount => _frames.Length;

    public AnimationFrame this[int key]
    {
        get => _frames[key];
        set => _frames[key] = value;
    }

    public bool TryGetFrame(int key, out AnimationFrame frame)
    {
        frame = _frames.ElementAtOrDefault(key);

        return frame != null;
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