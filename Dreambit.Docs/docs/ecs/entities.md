# Entities

Create entities through the active scene or the static convenience API:

```csharp
var player = CreateEntity(
    "player",
    tags: ["player"],
    createAt: new Vector3(40, 20, 0));

var weapon = Entity.CreateChildOf(player, "weapon");
```

The scene overload is clearer inside scene classes. `Entity.Create` is useful
inside components and helpers.

## Components

```csharp
var drawer = player.AttachComponent<SpriteDrawer>();
var collider = player.GetComponent<BoxCollider>();

if (player.GetComponentInChildren<MuzzleFlash>() is { } flash)
    flash.Enabled = true;

player.DetachComponent<SpriteAnimator>();
```

Attaching a type triggers its setup and lifecycle callbacks. Detaching is
deferred through the entity's component repository, so do not continue using a
detached component.

## Tags and lookup

```csharp
if (player.HasTag("player")) { }
var enemies = Scene.Instance.GetActiveEntitiesByTag("enemy");
var manager = Entity.FindByName("game-manager");
```

Tags are case-sensitive in runtime entity sets. Use a consistent lowercase
convention.

## Parent, enabled, and lifetime

Setting `Parent` changes transform interpretation and causes enabled state to be
inherited. `AlwaysUpdate` propagates to children and is suitable for cameras or
managers that must update outside normal visibility/activity handling.

Destroy with `Entity.Destroy(entity)`. Destruction includes descendants and is
processed by the scene. Use `Entity.IsDestroyed` or `Entity.IsNull` for guarded
references because a destroyed engine object intentionally compares like null.

