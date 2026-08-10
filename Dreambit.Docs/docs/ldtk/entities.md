# Reading LDtk entities

Dreambit exposes raw LDtk entity instances and provides import context for
positioning and render ordering. The default scene hook creates a generic ECS
entity, or loads an entity blueprint from a non-empty `Blueprint` string field.

`LDtkScene` invokes `OnLDtkEntityInstances` whenever a level is materialized:

```csharp
protected override void OnLDtkEntityInstances(
    LDtkLevelInstance level,
    IReadOnlyList<EntityInstance> entityInstances)
{
    foreach (var instance in entityInstances)
        Console.WriteLine($"Importing {instance._Identifier} from {level.Identifier}");
}
```

Override the hook when the game needs an identifier-based mapping, wants to
ignore marker entities, or needs to initialize custom components.

## Correctly positioning entities

The callback's `entityInstances` argument is a flat list of the included
layers. `LDtkLevelInstance` retains each entity's owning layer, the import
options, and the converted placement. Parent game entities to
`level.RootEntity`; the root already contains the level's world position.

```csharp
using System.Collections.Generic;
using Dreambit;
using Dreambit.LDtk;
using Microsoft.Xna.Framework;

protected override LDtkImportOptions CreateLDtkImportOptions() => new()
{
    PixelsPerUnit = 16f,
};

protected override void OnLDtkEntityInstances(
    LDtkLevelInstance level,
    IReadOnlyList<EntityInstance> entityInstances)
{
    foreach (var instance in entityInstances)
    {
        var entity = CreateEntity(instance._Identifier);
        entity.Parent = level.RootEntity;
        entity.Transform.Position2D = level.GetLocalPosition(instance);

        // Apply the LDtk layer to current drawable components.
        level.ApplyDrawLayer(entity, instance);

        // The entity is disabled and destroyed when this level streams out.
        level.TrackEntity(entity);
    }
}
```

`instance.Px` identifies the entity pivot, not necessarily its top-left corner.
If the Dreambit component or blueprint uses a top-left origin, subtract
`instance.Width * instance._Pivot.X` and
`instance.Height * instance._Pivot.Y` before converting to world units. If its
sprite uses the same pivot as LDtk, use the pivot position directly.

Use `instance.Layer` to inspect the owning raw `LayerInstance`,
`level.GetDrawLayer(instance)` for its computed Dreambit draw layer, and
`level.GetWorldPosition(instance)` when the entity should not be parented to the
level root. `ApplyDrawLayer` also updates drawable components in an existing
blueprint child hierarchy by default. Components attached later can set
`DrawLayer = level.GetDrawLayer(instance)` directly.

## Mapping identifiers to blueprints

A typical game maps stable LDtk entity identifiers onto Dreambit blueprints:

```csharp
private EntityBlueprint? GetBlueprint(EntityInstance instance)
{
    var assetName = instance._Identifier switch
    {
        "PlayerStart" => "blueprints/player",
        "Enemy" => "blueprints/enemy",
        "Treasure" => "blueprints/treasure",
        _ => null,
    };

    return assetName is null
        ? null
        : Resources.LoadAsset<EntityBlueprint>(assetName);
}
```

Use that result in the positioning loop:

```csharp
var blueprint = GetBlueprint(instance);
if (blueprint is null)
    continue;

var entity = CreateChildOfEntity(
    blueprint,
    level.RootEntity,
    createAt: level.GetLocalPosition(instance).ToVector3());
level.ApplyDrawLayer(entity, instance);
level.TrackEntity(entity);
```

Returning `null` for an unknown identifier makes it possible to add LDtk-only
markers without creating runtime objects. During development, a warning or
exception may be preferable so misspelled identifiers are noticed immediately.

## Reading fields

```csharp
foreach (var layer in level.LayerInstances ?? [])
{
    foreach (var instance in layer.EntityInstances ?? [])
    {
        Console.WriteLine($"{instance._Identifier}: {instance.Iid}");

        foreach (var field in instance.FieldInstances ?? [])
            Console.WriteLine($"  {field._Identifier}: {field._Value}");
    }
}
```

`FieldInstance.GetValue<T>()` deserializes a raw field value on demand.
`ResolveFilePath()` resolves FilePath fields relative to the LDtk project.
EntityRef fields can be followed with `ResolveEntityReference()` or parsed with
`TryGetEntityReference` and resolved later through `LDtkFile.ResolveEntity`.

For example:

```csharp
var healthField = instance.FieldInstances.FirstOrDefault(field =>
    field._Identifier == "Health");
var health = healthField?.GetValue<int>() ?? 1;

var configField = instance.FieldInstances.FirstOrDefault(field =>
    field._Identifier == "ConfigFile");
string? configPath = configField?.ResolveFilePath();

var targetField = instance.FieldInstances.FirstOrDefault(field =>
    field._Identifier == "Target");
EntityInstance? target = null;
if (targetField is not null &&
    targetField.TryGetEntityReference(out var targetReference))
    target = instance.Project.ResolveEntity(targetReference);
```

Check for a missing or null field before calling a resolver. An EntityRef may
cause its external target level to be deserialized, but it does not materialize
that level into the current scene.

## Ownership during streaming

Always call `level.TrackEntity` for an object whose lifetime belongs to the
loaded level. This includes entities created from child blueprints. The root
blueprint entity is enough when its descendants are owned and destroyed through
that hierarchy.

Do not track persistent objects such as the player, global managers, or UI. If
an LDtk marker moves the persistent player, update the existing player and leave
it owned by the scene or game session.

Entity definitions remain available through `instance.Definition`; tileset
references on definitions and layers are connected to their corresponding
`TilesetDefinition` objects.

## Converting to `LDtkEntity`

`LDtkEntity` is a MonoGame-friendly snapshot that retains every custom field as
raw JSON keyed by its LDtk identifier:

```csharp
LDtkEntity entityData = level.CreateEntityData(instance);

int health = entityData.GetField<int>("Health");
string? dialogue = entityData.GetField<string>("Dialogue");
LdtkColor? rawTint = entityData.GetField<LdtkColor>("Tint");
Color? tint = rawTint?.ToColor();
GridPoint? waypoint = entityData.GetField<GridPoint>("Waypoint");

int damage = entityData.GetField("Damage", defaultValue: 1);

if (entityData.TryGetField<string>("DisplayName", out var displayName))
    Console.WriteLine(displayName ?? "Unnamed");
```

`GetField<T>` uses the same JSON configuration as the project loader, including
LDtk point, vector, and color converters. The single-argument overload logs and
returns `default(T)` when the field is missing, null, or incompatible. The
fallback overload returns the supplied value instead.

`TryGetField<T>` does not log. It returns `false` when the identifier is missing
or the JSON cannot be converted to `T`. An existing field whose JSON value is
explicitly `null` returns `true` with `default(T)` in the output.

`CreateEntityData` converts `Position` and `Size` with the level's
`PixelsPerUnit`, includes total layer offsets in `Position`, and fills `Layer`
and `DrawLayer`. `PixelPosition` and `PixelSize` retain the source-pixel values.
Calling `LDtkEntity.FromInstance(instance)` directly remains a raw, one-pixel-
per-unit conversion for tooling that does not have a loaded level instance.
