# SoundEmitter2d

!!! warning "Obsolete"
    `SoundEmitter2d` is marked `[Obsolete]`. Prefer
    [`SoundEffectEmitter`](sound-effect-emitter.md) for new code.

The legacy component loads one `SoundEffect`, tracks its entity position and an
optional `Mover` velocity, and can call `Play` or `Play3D`.

```csharp
#pragma warning disable CS0612
var emitter = entity.AttachComponent<SoundEmitter2d>();
emitter.SoundEffectPath = "Audio/engine";
emitter.Volume = 0.5f;
emitter.Play();
#pragma warning restore CS0612
```

`Play3D` assumes at least one registered legacy listener. It is retained for
existing content, not recommended as the foundation for new audio work.

