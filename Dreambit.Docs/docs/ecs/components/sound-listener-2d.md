# SoundListener2d

!!! warning "Obsolete"
    `SoundListener2d` is marked `[Obsolete]` and only supports the legacy
    `SoundEmitter2d` 3D-audio path.

The component registers a MonoGame `AudioListener`, follows its entity's world
position, and uses `Mover.Velocity` when available. It unregisters when removed,
disabled, or destroyed.

Existing games that call `SoundEmitter2d.Play3D` need at least one listener.
New games should use `SoundEffectEmitter` until a replacement positional-audio
API is introduced.
