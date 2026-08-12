using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Dreambit.ECS;
using Newtonsoft.Json;

namespace Dreambit;

[DreambitAssetType("dreambit.audio.sound-cue")]
public class SoundCue : DreambitAsset
{
    [DreambitSerialize]
    [JsonProperty("loop")] public bool Loop;

    /// <summary>
    ///     Max distance audio can be heard
    /// </summary>
    [DreambitSerialize]
    [JsonProperty("max_audible_distance")] public float MaxAudibleDistance = 900f;

    /// <summary>
    ///     Maximum number of sound effect instances allowed at once
    /// </summary>
    [DreambitSerialize]
    [JsonProperty("max_overlaps")] public int MaxOverlaps = int.MaxValue;

    [DreambitSerialize]
    [JsonProperty("pan")] public float Pan;
    [DreambitSerialize]
    [JsonProperty("pitch")] public float Pitch;
    [DreambitSerialize]
    [JsonProperty("pitch_jitter")] public Vector2 PitchJitter = Vector2.Zero;

    /// <summary>
    ///     Where volume == 1 before falloff
    /// </summary>
    [DreambitSerialize]
    [JsonProperty("ref_distance")] public float RefDistance = 120f;

    /// <summary>
    ///     if already playing, restart same queue?
    /// </summary>
    [DreambitSerialize]
    [JsonProperty("restart_if_playing")] public bool RestartIfPlaying;

    [DreambitSerialize]
    [JsonProperty("takes")] public string[] Takes = [];

    [DreambitSerialize]
    [JsonProperty("volume")] public float Volume = 1.00f;
    [DreambitSerialize]
    [JsonProperty("volume_jitter")] public Vector2 VolumeJitter = Vector2.Zero;
    [JsonIgnore] public SoundEffect[] SfxTakes { get; internal set; }

    internal void LoadInternal()
    {
        SfxTakes = new SoundEffect[Takes.Length];

        for (var i = 0; i < SfxTakes.Length; i++)
        {
            var take = Takes[i];
            var sfx = Resources.LoadAsset<SoundEffect>(take);
            SfxTakes[i] = sfx;
        }
    }

    public SoundEffectInstance GetSfxInstance()
    {
        if (SfxTakes.Length == 1 && Takes.Length == 1)
            return SfxTakes[0]?.CreateInstance();

        var index = Random.Shared.Next(SfxTakes.Length);
        return SfxTakes[index]?.CreateInstance();
    }
}
