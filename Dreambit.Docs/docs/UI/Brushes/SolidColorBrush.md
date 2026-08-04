# `SolidColorBrush`

Fills the complete destination rectangle with the owner-supplied tint.

**Status:** XML brush element: `<SolidColorBrush>`  
**Namespace:** `Dreambit.UI`  
**Source:** `DreambitEngine/UI/Brushes/SolidColorBrush.cs`  
**Validated against:** DreambitEngine `main` / `ef6e5b9c600ad6e215c53ea287a0c7858884ce00`

## Inheritance

`IUiBrush` → `UiBrush` → `SolidColorBrush`

## XML example

```xml
<Border background-tint="#223047">
    <Border.Background>
        <SolidColorBrush />
    </Border.Background>
</Border>
```

## C# example

```csharp
border.Background = new SolidColorBrush();
border.BackgroundTint = new Color(34, 48, 71);
```

## Rendering and lifecycle behavior

- `MinimumSize` is 1×1.
- Uses one filled-rectangle draw operation.

## Production pitfalls

- The brush has no color property; set the owning element's tint such as `background-tint`, `track-tint`, or `fill-tint`.

## See also

- [`OutlineBrush`](./OutlineBrush.md)
- [`LayeredBrush`](./LayeredBrush.md)

---

_Source reviewed 2026-08-03. This page documents current implemented behavior, not a proposed API._
