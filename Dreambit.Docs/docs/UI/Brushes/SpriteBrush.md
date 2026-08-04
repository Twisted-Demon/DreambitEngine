# `SpriteBrush`

Stretches one sprite source rectangle to the owning bounds.

**Status:** XML brush element: `<SpriteBrush>`  
**Namespace:** `Dreambit.UI`  
**Source:** `DreambitEngine/UI/Brushes/SpriteBrush.cs`  
**Validated against:** DreambitEngine `main` / `ef6e5b9c600ad6e215c53ea287a0c7858884ce00`

## Inheritance

`IUiBrush` → `UiBrush` → `SpriteBrush`

## Declared API

| Member | Type | Behavior |
|---|---|---|
| `SpritePath` | `string` | Sprite asset path. |

## XML attributes

| Attribute | Type | Default | Meaning |
|---|---|---|---|
| `sprite` | `string` | required | Non-empty sprite path. |

## XML example

```xml
<Button.Background>
    <SpriteBrush sprite="Ui/button-flat.sprite" />
</Button.Background>
```

## C# example

```csharp
button.Background = new SpriteBrush("Ui/button-flat.sprite");
```

## Rendering and lifecycle behavior

- `MinimumSize` is the source rectangle size after dependency resolution.
- `Draw` stretches the source rectangle to destination bounds.

## Production pitfalls

- Missing/empty XML `sprite` throws `XmlException`.
- Stretching can distort aspect ratio and pixel borders. Use `UiViewbox` or `NineSliceBrush` when appropriate.

## See also

- [`UiTexture`](../Elements/UiTexture.md)
- [`NineSliceBrush`](./NineSliceBrush.md)
- [`TiledSpriteBrush`](./TiledSpriteBrush.md)

---

_Source reviewed 2026-08-03. This page documents current implemented behavior, not a proposed API._
