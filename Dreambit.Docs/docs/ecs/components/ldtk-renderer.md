# LDtkRenderer

`LDtkRenderer` draws a loaded `LDtkLevel` through the bundled LDtk renderer. An
`LDtkScene` creates and configures this component automatically.

```csharp
var renderer = CreateEntity("LDtkRenderer")
    .AttachComponent<LDtkRenderer>();
renderer.Level = level;
renderer.Entity.AlwaysUpdate = true;
```

Prefer the scene-managed path because `LDtkManager` and its renderer must already
be initialized. The component always reports visible and renders the level's
prerendered representation.

