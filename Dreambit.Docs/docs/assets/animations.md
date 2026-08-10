# Sprite animation

`SpriteSheetAnimation` selects a frame range from a sprite sheet and gives it a
frame rate, pivot, loop mode, and optional per-frame events.

The properties consumed by the current runtime type are:

```json
{
  "frame_rate": 8,
  "sprite_sheet_path": "SpriteSheets/player",
  "one_shot": false,
  "index_start": 0,
  "index_end": 5,
  "pivot": [15, 15],
  "frame_overrides": [
    {
      "frame_index": 2,
      "pivot": [16, 15],
      "event": { "name": "footstep", "args": {} }
    }
  ]
}
```

Both the animation-level `pivot` and each frame override use pixel coordinates
relative to a sprite-sheet cell. They are passed directly to `SpriteDrawer` and
converted to world space at draw time using the sprite's `PixelsPerUnit` and
world scale.

Load through `SpriteAnimator.AnimationPath`, then register events and play. See
the [`SpriteAnimator` component](../ecs/components/sprite-animator.md).

!!! warning "Current frame-range limitation"
    `SpriteSheetAnimation.Initialize` indexes its internal frame array with the
    source frame index. Keep `index_start` at `0` in current content. Also use
    the `frame_overrides` property shown above; older example content using a
    `frames` property does not match the current serialized member name.

