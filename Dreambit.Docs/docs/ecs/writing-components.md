# Writing components

Derive gameplay behavior from `Component` and override only the callbacks you
need:

```csharp
public sealed class Lifetime : Component
{
    public float Seconds { get; set; } = 2f;

    public override void OnUpdate()
    {
        Seconds -= Time.DeltaTime;
        if (Seconds <= 0f)
            Entity.Destroy(Entity);
    }
}
```

Every component can access `Entity`, `Transform`, `Scene`, a protected `Logger`,
and a protected `CoroutineService`.

Use public properties for values that should be configurable from
[entity blueprints](blueprints.md). When a component needs other components,
declare them with `[Require]` and optionally inject them with `[FromRequired]`.

Set `Enabled = false` to stop normal component participation and invoke
`OnDisabled`; set it back to true for `OnEnabled`. Clean up event subscriptions
and owned resources in `OnDestroyed`.

For visible behavior derive from `DrawableComponent` and implement `Bounds` plus
the appropriate draw hook. See [Drawing sprites and primitives](../rendering/drawing.md).

