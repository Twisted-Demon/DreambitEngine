# Sound cues

A `SoundCue` chooses from one or more audio takes and configures playback:

```json
{
  "takes": ["Audio/laser-1", "Audio/laser-2"],
  "volume": 0.8,
  "volume_jitter": [0.05, 0.05],
  "pitch": 0.0,
  "pitch_jitter": [0.08, 0.08],
  "pan": 0.0,
  "loop": false,
  "max_overlaps": 4,
  "restart_if_playing": false,
  "ref_distance": 120,
  "max_audible_distance": 900
}
```

`SoundEffectEmitter.Play` chooses a random take, applies cue and master volume,
jitter, pitch, pan, looping, and overlap limits.

```csharp
var cue = Resources.LoadAsset<SoundCue>("Audio/laser-cue");
emitter.Play(cue);
```

Pitch and volume jitter vectors represent how far below and above the base value
the random range extends. Keep resulting MonoGame volume and pitch values within
valid ranges; the emitter's cue path does not clamp every jittered value.

Distance fields and `restart_if_playing` are present in the asset but are not
used by the current `SoundEffectEmitter` playback implementation.
