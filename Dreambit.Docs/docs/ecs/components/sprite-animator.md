# SpriteAnimator

`SpriteAnimator` advances a `SpriteSheetAnimation` and updates the entity's
required sprite drawer.

```csharp
var animator = entity.AttachComponent<SpriteAnimator>();
animator.AnimationPath = "Animations/player_run";
animator.PlaySpeed = 1f;
animator.PlayOnStart = true;
animator.RegisterEvent("footstep", PlayFootstep);
animator.Play();
```

Control playback with `Play`, `Pause`, `Stop`, and `ResetAndPlay`. Use
`SetAnimation(path)` to switch immediately, or `QueueAnimation(asset)` to append
an animation. `ClearAnimationQueue` removes pending animations.

Subscribe to `OnAnimationEnded` for completion. Deregister named animation events
when the owning gameplay behavior no longer needs them.

The component relies on animation and sprite-sheet assets described in
[Sprite animation](../../assets/animations.md).

