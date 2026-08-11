# Sprite animation

`SpriteSheetAnimation` is an ordered sequence of sprites from a `SpriteSheet`.
The simplest form is a compact list of sprite-sheet indices:

```json
{
  "sprite_sheet": "SpriteSheets/player",
  "frames": [0, 1, 2, 3, 2, 1],
  "frames_per_second": 8,
  "loop": true,
  "pivot": [15, 15]
}
```

`sprite_sheet` is a normal Dreambit asset reference. In the Asset Editor, drag a
sprite-sheet asset onto this field or enter its project-relative asset path.

Frames are played in the exact order listed, so indices can start anywhere, be
repeated, or be arranged non-sequentially. A frame that needs extra behavior can
use the detailed form:

```json
{
  "sprite_sheet": "SpriteSheets/player",
  "frames": [
    0,
    1,
    {
      "sprite": 2,
      "duration": 0.2,
      "pivot": [16, 15],
      "event": {
        "name": "footstep",
        "args": { "surface": "stone" }
      }
    },
    3
  ],
  "frames_per_second": 8,
  "loop": false,
  "pivot": [15, 15]
}
```

Detailed frame properties are optional except for `sprite`:

- `duration` overrides the default `1 / frames_per_second` duration, in seconds.
- `pivot` overrides the animation pivot for that frame, in sprite-local pixels.
- `event` dispatches a named event when the frame becomes active.

The loader validates required fields, timing, frame indices, and event names and
reports all discovered errors together.
