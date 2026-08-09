# Entity blueprints

Blueprints create entity hierarchies and configure components from JSON. They
are useful for enemies, projectiles, pickups, and other repeatable game objects.

```json
{
  "name": "player_ship",
  "tags": ["player"],
  "position": [100, 80, 0],
  "components": [
    {
      "type": "Dreambit.ECS.SpriteDrawer, Dreambit",
      "properties": {
        "SpritePath": "Sprites/player"
      }
    },
    {
      "type": "Dreambit.ECS.BoxCollider, Dreambit",
      "properties": {
        "Bounds": [[-8,-8], [8,-8], [8,8], [-8,8]]
      }
    }
  ],
  "children": []
}
```

Load and spawn it:

```csharp
var blueprint = Resources.LoadAsset<EntityBlueprint>("Blueprints/player_ship");
var player = Entity.Create(blueprint, createAt: spawnPosition);
```

Properties are matched case-insensitively and must refer to writable component
members marked with `[DreambitSerialize]`. Built-in converters handle vectors,
colors, rectangles, points, box/polygon shapes, and references to Dreambit
assets. Component `[Require]` declarations are included when creation order is
resolved.

Prefer `[BlueprintType("StableName")]` on reusable components. Fully-qualified
assembly names also work but couple content more tightly to code structure.
Validate a blueprint hierarchy with `BlueprintValidator.ValidateOrThrow` when
building content tools or tests.

