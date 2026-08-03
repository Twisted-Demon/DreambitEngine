# Drawing sprites and primitives

The normal route is an ECS drawable:

```csharp
var sprite = entity.AttachComponent<SpriteDrawer>()
    .WithSprite("Sprites/hero")
    .WithPivot(PivotType.BottomCenter);
sprite.DrawLayer = 10;
```

All drawables expose `DrawLayer` and optional `Effect`. They implement `Bounds`
for camera culling and draw through `OnPreDraw`, `OnDraw`, and `OnPostDraw`.
`OnDrawUi` is reserved for the UI pass.

For a custom visible component:

```csharp
public sealed class BeamDrawer : DrawableComponent
{
    public override RectangleF Bounds => _bounds;

    public override void OnDraw()
    {
        Core.SpriteBatch.DrawLine(start, end, Color.Cyan, 2f);
    }
}
```

Dreambit provides SpriteBatch extensions for world sprites, points, lines,
polygons, filled rectangles, and rings. Use `Scene.MainCamera` helpers so texture
pixels, world units, pivots, and zoom remain consistent.

`RenderingOptions.SamplerState` defaults to `PointClamp`, which suits pixel art.
Choose a filtered sampler for smooth scaled art. UI has a separate sampler.

