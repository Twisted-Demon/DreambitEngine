# Audio

For new gameplay audio, load a `SoundCue` and play it through
`SoundEffectEmitter`:

```csharp
var emitter = entity.AttachComponent<SoundEffectEmitter>();
var cue = Resources.LoadAsset<SoundCue>("Audio/explosion-cue");
emitter.Play(cue);
```

The emitter pools active `SoundEffectInstance` objects, enforces the cue's
overlap limit, and disposes stopped instances. Looping cues use one primary
instance. Control an emitter with `MasterVolume`, `PauseAll`, `ResumeAll`, and
`StopAll`.

See [Sound cues](../assets/sound-cues.md) for authoring and the
[`SoundEffectEmitter` page](../ecs/components/sound-effect-emitter.md) for the
component API.

`SoundEmitter2d` and `SoundListener2d` are obsolete legacy components. Their
MonoGame positional-audio path remains for old code, but current cue playback is
not positional and does not apply the cue's distance settings.

Dreambit can also load `Song` assets through the audio baker/loader. Use
MonoGame's media APIs to control song playback; the current ECS audio component
is for sound effects.

