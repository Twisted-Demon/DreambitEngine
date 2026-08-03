# SpriteDrawer

`SpriteDrawer` renders one `Sprite` at the entity's world transform.

```csharp
var drawer = entity.AttachComponent<SpriteDrawer>()
    .WithSprite("Sprites/player")
    .WithTint(Color.White)
    .WithOpacity(1f)
    .WithPivot(PivotType.Center);

drawer.FlipX = true;
drawer.DrawLayer = 10;
```

You can also assign `Sprite`, call `SetSprite`, or use a custom normalized pivot
with `WithPivot(Vector2)`. `(0,0)` is top-left and `(1,1)` is bottom-right.

`Bounds` accounts for the sprite, pivot, world scale, and camera units and is
used for culling. `DrawLayer` orders drawable groups; within normal lit rendering
the pipeline also considers effect and world Y.

`Effect` comes from `DrawableComponent` and selects a custom MonoGame effect.
Use the default when you want Dreambit's normal 2D lighting path.

