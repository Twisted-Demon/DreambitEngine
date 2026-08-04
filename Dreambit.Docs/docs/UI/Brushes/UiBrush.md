# `UiBrush`

Convenient abstract base with no-op parsing/dependency hooks and zero minimum size.

**Status:** Interface/abstract type; not instantiated directly in XML  
**Namespace:** `Dreambit.UI`  
**Source:** `DreambitEngine/UI/Brushes/UiBrush.cs`  
**Validated against:** DreambitEngine `main` / `ef6e5b9c600ad6e215c53ea287a0c7858884ce00`

## Inheritance

`IUiBrush` → `UiBrush`

## Methods

| Member | Behavior |
|---|---|
| `MinimumSize` | Virtual; defaults to `Point.Zero`. |
| `Parse(XmlNode)` | Virtual no-op. |
| `ResolveDependencies()` | Virtual no-op. |
| `Draw(Rectangle, Color)` | Abstract. |

## C# example

```csharp
public sealed class CheckerBrush : UiBrush
{
    public override void Draw(Rectangle bounds, Color tint)
    {
        // Draw using already-resolved resources only.
    }
}
```

## Implementing a custom brush

- Derive from this class for most custom brushes.
- Override only the hooks your brush needs.

## See also

- [`IUiBrush`](./IUiBrush.md)
- [`SolidColorBrush`](./SolidColorBrush.md)

---

_Source reviewed 2026-08-03. This page documents current implemented behavior, not a proposed API._
