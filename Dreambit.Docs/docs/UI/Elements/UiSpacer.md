# `UiSpacer`

Invisible, non-interactive element used to reserve layout space.

**Status:** XML tag: `Spacer`  
**Namespace:** `Dreambit.UI`  
**Source:** `DreambitEngine/UI/Elements/UiSpacer.cs`  
**Validated against:** DreambitEngine `main` / `ef6e5b9c600ad6e215c53ea287a0c7858884ce00`

## Inheritance

`UiElement` → `UiSpacer`

### Methods

| Member | Behavior |
|---|---|
| `UiSpacer(int width, int height)` | Creates a fixed-size spacer in C#. |

## Common inherited XML attributes

| XML attribute | Type | XML default | Meaning |
|---|---|---|---|
| `id` | `string` | empty | Case-sensitive lookup ID used by `UiLayout.Find` and `GetRequired<T>`. |
| `x` | `UiLength` | `0%` | Horizontal offset resolved against the parent width. |
| `y` | `UiLength` | `0%` | Vertical offset resolved against the parent height. |
| `width` | `UiLength` | `100%` | Fixed pixels, percentage, or `*` for automatic desired width. |
| `height` | `UiLength` | `100%` | Fixed pixels, percentage, or `*` for automatic desired height. |
| `anchor` | `UiAnchor` | `TopLeft` | Reference point on the parent. |
| `origin` | `UiAnchor` | `TopLeft` | Point on this element placed at the anchored position. |
| `z` | `int` | `0` | Sibling draw order. Higher values draw and hit-test above lower values. |
| `grid-row` | `int` | `0` | Zero-based row used by a `UiGrid` parent. |
| `grid-column` | `int` | `0` | Zero-based column used by a `UiGrid` parent. |
| `grid-row-span` | `int` | `1` | Grid row span, clamped to at least one. |
| `grid-column-span` | `int` | `1` | Grid column span, clamped to at least one. |
| `is-visible` | `bool` | `true` | Removes the element subtree from layout, drawing, and input when false. |
| `is-enabled` | `bool` | `true` | Disables input for the element subtree when false. |
| `is-hit-test-visible` | `bool` | type default | Controls whether this element can be the direct pointer target. |
| `is-focusable` | `bool` | type default | Controls keyboard/controller focus eligibility. |
| `captures-keyboard-input` | `bool` | type default | Consumes keyboard availability while focused. |
| `clip-to-bounds` | `bool` | `false` | Clips descendants to this element's bounds using the UI scissor stack. |

## XML example

```xml
<HorizontalStackPanel width="100%" height="40">
    <Text width="*" text="Credits" font="monogram" />
    <Spacer width="16" height="1" />
    <Text width="*" text="1,250" font="monogram" />
</HorizontalStackPanel>
```

## C# example

```csharp
stack.AddChild(new UiSpacer(width: 16, height: 1));
```

## Layout behavior

- Natural content size is zero; authored width and height provide the reserved space.

## Production pitfalls

- XML common defaults are `width="100%"` and `height="100%"`. Always specify the dimension you intend a spacer to reserve.
- A spacer is usually unnecessary when the parent already supports `spacing`, padding, or grid tracks.

## See also

- [`UiStackPanelBase`](UiStackPanelBase.md)
- [`UiGrid`](UiGrid.md)

---

_Source reviewed 2026-08-03. This page documents current implemented behavior, not a proposed API._
