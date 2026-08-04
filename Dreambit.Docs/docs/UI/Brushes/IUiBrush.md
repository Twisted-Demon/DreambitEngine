# `IUiBrush`

Contract for reusable visuals drawn inside bounds owned by UI elements.

**Status:** Interface/abstract type; not instantiated directly in XML  
**Namespace:** `Dreambit.UI`  
**Source:** `DreambitEngine/UI/Brushes/UiBrush.cs`  
**Validated against:** DreambitEngine `main` / `ef6e5b9c600ad6e215c53ea287a0c7858884ce00`

## Inheritance

`IUiBrush`

## Declared API

| Member | Type | Behavior |
|---|---|---|
| `MinimumSize` | `Point` | Smallest size at which the brush can render correctly. |

## Methods

| Member | Behavior |
|---|---|
| `Parse(XmlNode)` | Reads brush-specific XML. |
| `ResolveDependencies()` | Loads or refreshes external assets. |
| `Draw(Rectangle, Color)` | Draws inside destination bounds using owner-supplied tint. |

## C# example

```csharp
public sealed class DiagonalBrush : UiBrush
{
    public override Point MinimumSize => new(1, 1);

    public override void Draw(Rectangle bounds, Color tint)
    {
        Graphics.SpriteBatch.DrawLine(
            bounds.Location.ToVector2(),
            new Vector2(bounds.Right, bounds.Bottom),
            tint);
    }
}
```

## Rendering and lifecycle behavior

- Brushes do not participate in the visual tree, layout routing, focus, or input.
- Owning controls use `MinimumSize` when measuring automatic dimensions.
- Brush type discovery uses loaded assemblies and class name or `UiXmlNameAttribute`.

## Implementing a custom brush

- Keep `Draw` bounded to the supplied rectangle.
- Return a meaningful `MinimumSize` when shrinking below a threshold would break the visual.
- Resolve assets outside `Draw`; never load content per frame.

## Production pitfalls

- A brush has no independent opacity/state unless its implementation supplies it; the owner passes one tint.
- Shared brush instances share mutable asset/configuration state. Treat them as immutable after setup when reused.

## See also

- [`UiBrush`](./UiBrush.md)
- [`UiContentControl`](../Elements/UiContentControl.md)

---

_Source reviewed 2026-08-03. This page documents current implemented behavior, not a proposed API._
