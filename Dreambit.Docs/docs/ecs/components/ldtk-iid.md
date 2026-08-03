# LDtkIid

The class named `LDtkIid` stores an LDtk entity instance GUID and registers the
Dreambit entity with `LDtkManager` during its lifecycle.

```csharp
var reference = entity.AttachComponent<LDtkIid>();
// Iid has an internal setter and is normally assigned by LDtk integration.
```

Do not use this as a general GUID component. It exists so LDtk-generated entity
setup can connect editor instances to runtime entities. See
[Mapping LDtk entities](../../ldtk/entities.md).

!!! note
    The source file is named `LDtkEntity.cs`, but the component type and its
    blueprint identifier are `LDtkIid`.

