using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dreambit;

public class SpriteSheetAnimation : DreambitAsset
{
    // Allow a null draft while the asset is being authored. Runtime loading remains strict through
    // GetValidationErrors() in SpriteSheetAnimationLoader.
    [JsonProperty("sprite_sheet", Required = Required.AllowNull)]
    public SpriteSheet SpriteSheet { get; set; }

    [JsonProperty("frames", Required = Required.Always)]
    public List<SpriteAnimationFrame> Frames { get; set; } = [];

    [JsonProperty("frames_per_second")]
    public float FramesPerSecond { get; set; } = 12f;

    [JsonProperty("loop")]
    public bool Loop { get; set; } = true;

    /// <summary>
    /// The default pivot in sprite-local pixels. Individual frames can override it.
    /// </summary>
    [JsonProperty("pivot")]
    public Vector2 Pivot { get; set; }

    [JsonIgnore]
    public int FrameCount => Frames?.Count ?? 0;

    [JsonIgnore]
    public float Duration
    {
        get
        {
            if (Frames is null)
                return 0f;

            var duration = 0f;
            for (var i = 0; i < Frames.Count; i++)
                duration += GetFrameDuration(i);
            return duration;
        }
    }

    public SpriteAnimationFrame this[int index] => Frames[index];

    public bool TryGetFrame(int index, out SpriteAnimationFrame frame)
    {
        if (index >= 0 && index < FrameCount)
        {
            frame = Frames[index];
            return true;
        }

        frame = null;
        return false;
    }

    public float GetFrameDuration(int index)
    {
        var frame = Frames[index];
        return frame.Duration ?? 1f / FramesPerSecond;
    }

    public Vector2 GetFramePivot(int index)
    {
        return Frames[index].Pivot ?? Pivot;
    }

    public IReadOnlyList<string> GetValidationErrors()
    {
        var errors = new List<string>();

        if (SpriteSheet is null)
            errors.Add("sprite_sheet is required.");
        if (Frames is null)
            errors.Add("frames is required.");
        else if (Frames.Count == 0)
            errors.Add("frames must contain at least one frame.");
        if (FramesPerSecond <= 0f)
            errors.Add("frames_per_second must be greater than zero.");

        if (Frames is null)
            return errors;

        for (var i = 0; i < Frames.Count; i++)
        {
            var frame = Frames[i];
            if (frame is null)
            {
                errors.Add($"frames[{i}] cannot be null.");
                continue;
            }

            if (frame.SpriteIndex < 0)
                errors.Add($"frames[{i}].sprite cannot be negative.");
            else if (SpriteSheet is not null && frame.SpriteIndex >= SpriteSheet.FrameCount)
                errors.Add($"frames[{i}].sprite ({frame.SpriteIndex}) exceeds the sprite sheet's {SpriteSheet.FrameCount} frames.");

            if (frame.Duration is <= 0f)
                errors.Add($"frames[{i}].duration must be greater than zero when specified.");
            if (frame.Event is not null && string.IsNullOrWhiteSpace(frame.Event.Name))
                errors.Add($"frames[{i}].event.name is required.");
        }

        return errors;
    }
}

[JsonConverter(typeof(SpriteAnimationFrameConverter))]
public class SpriteAnimationFrame
{
    [JsonProperty("sprite", Required = Required.Always)]
    public int SpriteIndex { get; set; }

    /// <summary>
    /// Optional duration in seconds. When omitted, the animation's frame rate is used.
    /// </summary>
    [JsonProperty("duration", NullValueHandling = NullValueHandling.Ignore)]
    public float? Duration { get; set; }

    /// <summary>
    /// Optional pivot in sprite-local pixels. When omitted, the animation pivot is used.
    /// </summary>
    [JsonProperty("pivot", NullValueHandling = NullValueHandling.Ignore)]
    public Vector2? Pivot { get; set; }

    [JsonProperty("event", NullValueHandling = NullValueHandling.Ignore)]
    public SpriteAnimationEvent Event { get; set; }
}

public class SpriteAnimationEvent
{
    [JsonProperty("name", Required = Required.Always)]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("args")]
    public Dictionary<string, string> Args { get; set; } = [];
}

public sealed class SpriteAnimationFrameConverter : JsonConverter<SpriteAnimationFrame>
{
    public override SpriteAnimationFrame ReadJson(
        JsonReader reader,
        Type objectType,
        SpriteAnimationFrame existingValue,
        bool hasExistingValue,
        JsonSerializer serializer)
    {
        var token = JToken.Load(reader);
        if (token.Type == JTokenType.Integer)
            return new SpriteAnimationFrame { SpriteIndex = token.Value<int>() };

        if (token is not JObject frameObject)
            throw new JsonSerializationException(
                $"Animation frames must be a sprite index or an object, but found {token.Type}.");

        var spriteToken = frameObject["sprite"];
        if (spriteToken?.Type != JTokenType.Integer)
            throw new JsonSerializationException("Detailed animation frames require an integer 'sprite' property.");

        return new SpriteAnimationFrame
        {
            SpriteIndex = spriteToken.Value<int>(),
            Duration = frameObject["duration"]?.Value<float>(),
            Pivot = frameObject["pivot"]?.ToObject<Vector2>(serializer),
            Event = frameObject["event"]?.ToObject<SpriteAnimationEvent>(serializer)
        };
    }

    public override void WriteJson(
        JsonWriter writer,
        SpriteAnimationFrame value,
        JsonSerializer serializer)
    {
        if (value.Duration is null && value.Pivot is null && value.Event is null)
        {
            writer.WriteValue(value.SpriteIndex);
            return;
        }

        writer.WriteStartObject();
        writer.WritePropertyName("sprite");
        writer.WriteValue(value.SpriteIndex);

        if (value.Duration is not null)
        {
            writer.WritePropertyName("duration");
            writer.WriteValue(value.Duration.Value);
        }

        if (value.Pivot is not null)
        {
            writer.WritePropertyName("pivot");
            serializer.Serialize(writer, value.Pivot.Value);
        }

        if (value.Event is not null)
        {
            writer.WritePropertyName("event");
            serializer.Serialize(writer, value.Event);
        }

        writer.WriteEndObject();
    }
}
