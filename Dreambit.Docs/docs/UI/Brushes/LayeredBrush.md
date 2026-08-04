# `LayeredBrush`

Back-to-front collection of brushes drawn into the same rectangle.

**Status:** XML brush element: `<LayeredBrush>`  
**Namespace:** `Dreambit.UI`  
**Source:** `DreambitEngine/UI/Brushes/LayeredBrush.cs`  
**Validated against:** DreambitEngine `main` / `ef6e5b9c600ad6e215c53ea287a0c7858884ce00`

## Inheritance

`IUiBrush` → `UiBrush` → `LayeredBrush`

## Declared API

| Member | Type | Behavior |
|---|---|---|
| `Brushes` | `IList<IUiBrush>` | Back-to-front brush order. |

## XML example

```xml
<Button.Background>
    <LayeredBrush>
        <NineSliceBrush sprite="Ui/button.sprite" slice="6" />
        <OutlineBrush thickness="1" />
    </LayeredBrush>
</Button.Background>
```

## C# example

```csharp
var layered = new LayeredBrush();
layered.Brushes.Add(new NineSliceBrush
{
    SpritePath = "Ui/button.sprite",
    SliceThickness = UiThickness.Uniform(6)
});
layered.Brushes.Add(new OutlineBrush
{
    Thickness = UiThickness.Uniform(1)
});
button.Background = layered;
```

## Rendering and lifecycle behavior

- `MinimumSize` is the component-wise maximum of child minimum sizes, not their sum.
- Dependencies and draws run in list order.
- The same owner tint is passed to every layer.

## Production pitfalls

- Per-layer tint is not supported by the current implementation.
- Draw costs are additive. A layered nine-slice plus outline is up to thirteen draw operations.
- Null entries are skipped at draw/measure time, but XML should contain valid brush elements.

## See also

- [`NineSliceBrush`](./NineSliceBrush.md)
- [`OutlineBrush`](./OutlineBrush.md)
- [`SolidColorBrush`](./SolidColorBrush.md)

---

_Source reviewed 2026-08-03. This page documents current implemented behavior, not a proposed API._
