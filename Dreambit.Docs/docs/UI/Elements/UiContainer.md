# `UiContainer`

General child-owning element and base class for layout containers.

**Status:** XML tag: `Container`  
**Namespace:** `Dreambit.UI`  
**Source:** `DreambitEngine/UI/Elements/UiContainer.cs`  
**Validated against:** DreambitEngine `main` / `ef6e5b9c600ad6e215c53ea287a0c7858884ce00`

## Inheritance

`UiElement` → `UiContainer`

## When to use

Use directly for simple absolute child composition, or derive from it when implementing a reusable multi-child layout policy.

## Declared API

### Properties and fields

| Member | Type | Behavior |
|---|---|---|
| `Children` | `List<UiElement>` (inherited field) | Owned child collection; mutate through container methods. |

### Methods

| Member | Behavior |
|---|---|
| `AddChild(UiElement)` | Validates ownership, layout compatibility, cycles, and IDs before attachment. |
| `RemoveChild(UiElement)` | Detaches one owned child and validates interaction state. |
| `ClearChildren()` | Detaches all owned children and invalidates layout. |

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
<Container width="100%" height="100%">
    <Text x="24" y="24" width="240" height="*"
          text="Absolute child" font="monogram" />
</Container>
```

## C# example

```csharp
var container = new UiContainer
{
    Width = UiLength.Percent(1f),
    Height = UiLength.Percent(1f)
};

container.AddChild(new UiText
{
    X = UiLength.Pixels(24),
    Y = UiLength.Pixels(24),
    Width = UiLength.Pixels(240),
    Text = "Absolute child"
});
```

## Layout behavior

- Natural size is the maximum child extent calculated from each visible child's resolved `X`/`Y` plus `DesiredSize`.
- Default arrangement preserves each child's own geometry inside this container's `Bounds`.

## Ownership and lifecycle

- An element may have only one parent and may not be added beneath one of its descendants.
- Attaching an element already connected to a different layout throws `InvalidOperationException`.
- Duplicate IDs are validated before attachment.

## Performance notes

- Bulk replacement should use `ClearChildren()` followed by additions; avoid rebuilding every frame.

## Production pitfalls

- Directly editing `Children` bypasses parent assignment, layout attachment, ID validation, event cleanup, and invalidation.
- Adding the same instance twice or creating a cycle throws immediately.

## See also

- [`UiElement`](UiElement.md)
- [`UiPanel`](UiPanel.md)
- [`UiCanvas`](UiCanvas.md)

---

_Source reviewed 2026-08-03. This page documents current implemented behavior, not a proposed API._
