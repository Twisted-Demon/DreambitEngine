# Mapping LDtk entities

`LDtkEntity<T>` is intended as a base for adapters from generated LDtk entity
data to Dreambit entities. Its helpers can create an entity at the LDtk position,
attach the source tile as a sprite, and build a polygon collider.

```csharp
public sealed class SpawnAdapter : LDtkEntity<GeneratedSpawn>
{
    protected override void SetUp(LDtkLevel level)
    {
        // Convert matching GeneratedSpawn data into Dreambit entities.
    }
}
```

`CreateEntity(data)` assigns the LDtk IID as the entity GUID and attaches
`LDtkIid`. `AttachTileSpriteDrawer` uses the manager's tileset sprite sheet.
`CreatePolyCollider` converts editor points into local collider vertices.

!!! warning "Experimental automatic discovery"
    Automatic mapping is not complete in the current implementation. The static
    generic `SetUpEntities` path obtains generated data and then attempts to cast
    each data item to the adapter type, so normal generated entities will not
    invoke `SetUp`. Treat these helpers as an integration starting point and
    manually enumerate `level.GetEntities<T>()` in scene code, or correct the
    adapter dispatch before relying on discovery.

The current `LDtkManager.RegisterEntity` guard also prevents a new IID from being
added to its reference map. Do not depend on IID lookup until that registration
path is corrected.

