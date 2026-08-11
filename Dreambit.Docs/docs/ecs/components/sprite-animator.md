# SpriteAnimator

`SpriteAnimator` plays a `SpriteSheetAnimation` through the entity's required
`SpriteDrawer`.

```csharp
var animator = entity.AttachComponent<SpriteAnimator>();
var run = Resources.LoadAsset<SpriteSheetAnimation>("Animations/player_run");

animator.RegisterEvent("footstep", animationEvent =>
{
    var surface = animationEvent.Args.GetValueOrDefault("surface", "default");
    PlayFootstep(surface);
});

animator.Play(run);
```

`SetAnimation(asset)` selects an animation and displays its first frame while
preserving the current play/pause state. Re-selecting the current asset is a
no-op, which makes it safe for state resolvers to call every update. Use
`Restart()` when an intentional restart is needed.

`Play()`, `Pause()`, and `Stop()` control playback. `Stop()` pauses and rewinds.
`Play(asset)` selects and plays in one call. String-path overloads remain
available for runtime-loaded animations.

`QueueAnimation(asset)` starts the queued animation when the current animation
reaches its end, even when the current animation normally loops. Queued
animations are initialized exactly like directly selected animations.

Subscribe to `AnimationCompleted` to observe the end of each animation
iteration. `CurrentFrame`, `CurrentFrameIndex`, `IsPlaying`, and
`NormalizedProgress` expose playback state.

Named frame handlers receive the complete `SpriteAnimationEvent`, including its
`Args` dictionary. Deregister a particular callback with
`DeregisterEvent(name, callback)`, or clear every callback for a name with
`DeregisterEvent(name)`.
