# Entity-component system

Dreambit uses composition: an `Entity` supplies identity, tags, hierarchy, and a
`Transform`; attached `Component` objects supply behavior, rendering, collision,
audio, and other capabilities.

```csharp
var enemy = CreateEntity(
        "scout",
        tags: ["enemy", "flying"],
        createAt: new Vector3(100, 80, 0))
    .AttachComponent<EnemyController>();
```

`AttachComponent<T>` returns the component, which makes fluent setup convenient.
An entity stores at most one component of a given type; attaching the same type
again returns the existing instance.

Read [Entities](entities.md), [Writing components](writing-components.md), and
[Transform](transform.md) first. Use [Requirements and injection](requirements.md)
to compose dependencies safely, then browse the component pages for built-in
behavior.

