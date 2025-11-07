using System;
using Microsoft.Xna.Framework.Audio;
using Newtonsoft.Json;

namespace Dreambit;

public class SoundCue : DreambitAsset
{
   [JsonIgnore] public SoundEffect[] SfxTakes { get; internal set; }
   
   [JsonProperty("takes")] public string[] Takes = [];
   [JsonProperty("loop")] public bool Loop = false;

   [JsonProperty("volume")] public float Volume = 1.00f;
   [JsonProperty("pitch")] public float Pitch = 0.00f;
   [JsonProperty("pan")] public float Pan = 0.00f;

   /// <summary>
   /// Max distance audio can be heard
   /// </summary>
   [JsonProperty("max_audible_distance")] public float MaxAudibleDistance = 900f;
   /// <summary>
   /// Where volume == 1 before falloff
   /// </summary>
   [JsonProperty("ref_distance")] public float RefDistance = 120f;

   /// <summary>
   /// Maximum number of sound effect instances allowed at once
   /// </summary>
   [JsonProperty("max_overlaps")] public int MaxOverlaps = int.MaxValue;

   /// <summary>
   /// if already playing, restart same queue?
   /// </summary>
   [JsonProperty("restart_if_playing")] public bool RestartIfPlaying = false;

   internal void LoadInternal()
   {
      SfxTakes = new SoundEffect[Takes.Length];

      for (int i = 0; i < SfxTakes.Length; i++)
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

      var randTake = new Random().Next(0,  SfxTakes.Length - 1);
      return SfxTakes[randTake]?.CreateInstance();
   }
}