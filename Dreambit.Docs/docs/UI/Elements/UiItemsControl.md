# `UiItemsControl`

Stack-based collection host for declared or programmatically generated UI elements.

**Status:** XML tag: `ItemsControl`  
**Namespace:** `Dreambit.UI`  
**Source:** `DreambitEngine/UI/Elements/UiItemsControl.cs`  
**Validated against:** DreambitEngine `main` / `ef6e5b9c600ad6e215c53ea287a0c7858884ce00`

## Inheritance

`UiElement` → `UiContainer` → `UiStackPanelBase` → `UiStackPanel` → `UiItemsControl`

## Declared API

### Properties and fields

| Member | Type | Behavior |
|---|---|---|
| `Items` | `IReadOnlyList<UiElement>` | View of current child items. |

### Methods

| Member | Behavior |
|---|---|
| `AddItem(UiElement)` | Appends one item. |
| `RemoveItem(UiElement)` | Removes and detaches one item. |
| `ClearItems()` | Removes all items. |
| `SetItems<T>(IEnumerable<T>, Func<T, UiElement>)` | Clears and materializes one UI element per data value. |

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
<ItemsControl id="objective-list"
              width="320" height="*"
              orientation="Vertical"
              spacing="6">
    <Text text="Reach wave 10" font="monogram" />
    <Text text="Keep the planet above 50%" font="monogram" />
</ItemsControl>
```

## C# example

```csharp
UiItemsControl list = layout.GetRequired<UiItemsControl>("objective-list");

list.SetItems(objectives, objective => new UiText
{
    Width = UiLength.Percent(1f),
    Height = UiLength.Auto(),
    Text = objective.Description,
    FontPath = "monogram",
    FontSize = 18f,
    HorizontalAlignment = HorizontalAlignment.Left
});
```

## Ownership and lifecycle

- Generated elements become owned children.
- The template must return a non-null, unattached element for every item.

## Performance notes

- Use for small and medium game UI collections.
- For high-frequency or large lists, update existing elements or implement a game-specific pooled/virtualized surface.

## Production pitfalls

- This is not data binding. Calling `SetItems` rebuilds all children.
- There is no item recycling or virtualization.
- Any event handlers attached by the template must follow the generated element lifetime.

## See also

- [`UiStackPanel`](UiStackPanel.md)
- [`UiSelector`](UiSelector.md)
- [`UiListBox`](UiListBox.md)

---

_Source reviewed 2026-08-03. This page documents current implemented behavior, not a proposed API._
