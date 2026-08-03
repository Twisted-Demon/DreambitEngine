# Entity blueprints

Entity blueprints are JSON assets that define identity, tags, transform,
components, and children. They are baked as JSON and loaded as
`EntityBlueprint`.

```csharp
var asset = Resources.LoadAsset<EntityBlueprint>("Blueprints/enemy");
var enemy = Entity.Create(
    asset,
    enabled: true,
    createAt: spawnPosition,
    rotation: null,
    scale: null);
```

Spawn arguments override the root blueprint's corresponding values. Child
blueprints retain their local transform and are parented into the resulting
hierarchy.

The component resolver accepts stable `[BlueprintType]` IDs or qualified CLR
type names. Property converters support common MonoGame/Dreambit values and
asset references. Requirements are created before dependents.

See [ECS entity blueprints](../ecs/blueprints.md) for the JSON shape and
component example.

