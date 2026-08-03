# Sprites and sprite sheets

A `Sprite` pairs a texture with a source rectangle. Author one as JSON:

```json
{
  "texture": "Textures/ui-atlas",
  "source": [0, 0, 48, 16]
}
```

Or create it at runtime:

```csharp
var sprite = Sprite.Create("Textures/ui-atlas", 0, 0, 48, 16);
drawer.SetSprite(sprite);
```

A sprite sheet splits a texture into equal cells:

```json
{
  "texture_path": "Textures/player-sheet",
  "columns": 6,
  "rows": 1
}
```

```csharp
var sheet = Resources.LoadAsset<SpriteSheet>("SpriteSheets/player");
drawer.SetSprite(sheet[0]);
if (sheet.TryGetFrame(frameIndex, out var frame))
    drawer.SetSprite(frame);
```

Frames are row-major. Texture dimensions should divide evenly by row/column
counts, because leftover pixels are not represented in the equal frame size.

`SpriteSheet.Create(gridSize, path)` infers columns and rows from a square cell
size and is used by LDtk tilesets.

