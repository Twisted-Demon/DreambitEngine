# PointLight2D

`PointLight2D` emits colored light around its world position.

```csharp
var light = CreateEntity("torch-light", createAt: torchPosition)
    .AttachComponent<PointLight2D>();

light.Radius = 160f;
light.Color = new Color(255, 170, 90);
light.Intensity = 1.2f;
```

`Radius` determines both influence and culling bounds. Dreambit's lighting
uniforms support up to 32 point lights in a rendered batch; keep the most useful
lights active and avoid creating one per tiny effect.

See [Lighting](../../rendering/lighting.md) for ambient setup and render-pipeline
behavior.

