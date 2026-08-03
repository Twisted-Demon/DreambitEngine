# SoundEffectEmitter

`SoundEffectEmitter` plays `SoundCue` assets and manages simultaneous instances.
It is the current general-purpose audio component.

```csharp
var emitter = entity.AttachComponent<SoundEffectEmitter>();
emitter.MasterVolume = 0.8f;

var cue = Resources.LoadAsset<SoundCue>("Audio/laser-cue");
emitter.Play(cue);
```

The cue controls takes, overlap limits, looping, volume, pitch, pan, and jitter.
Use `PauseAll`, `ResumeAll`, or `StopAll` for the emitter's instances. Destruction
stops and disposes them automatically.

`MasterVolume` is clamped to 0–1. `CullWhenOffscreen` currently exists as a
configuration property but is not applied by the component's playback path.
See [Sound cues](../../assets/sound-cues.md).

