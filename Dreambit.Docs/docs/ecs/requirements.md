# Requirements and injection

Declare component dependencies on the dependent component:

```csharp
[Require(typeof(BoxCollider), typeof(SpriteDrawer))]
public sealed class Projectile : Component
{
    [FromRequired] private BoxCollider _collider;
    [FromRequired] private SpriteDrawer _drawer;

    public override void OnCreated()
    {
        _collider.IsTrigger = true;
        _drawer.WithSprite("Sprites/projectile");
    }
}
```

When `Projectile` is attached, missing required types are attached first.
`[FromRequired]` then assigns matching fields or properties, including private
ones. The field type must match a component attached to the same entity.

Use this pattern when the dependency is essential. For an optional capability,
use `Entity.GetComponent<T>()` and handle null explicitly.

Requirements also determine a safe creation order for blueprint components.
Avoid circular requirements; the resolver reports cycles rather than guessing
an order.

