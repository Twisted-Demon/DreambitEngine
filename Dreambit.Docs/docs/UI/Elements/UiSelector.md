# `UiSelector`

Abstract items control with one selected child and navigation behavior.

**Status:** Abstract base type  
**Namespace:** `Dreambit.UI`  
**Source:** `DreambitEngine/UI/Elements/UiSelector.cs`  
**Validated against:** DreambitEngine `main` / `ef6e5b9c600ad6e215c53ea287a0c7858884ce00`

## Inheritance

`UiElement` → `UiContainer` → `UiStackPanelBase` → `UiStackPanel` → `UiItemsControl` → `UiSelector`

## Declared API

### Properties and fields

| Member | Type | Behavior |
|---|---|---|
| `SelectedIndex` | `int` | Selected child index or -1. |
| `SelectedItem` | `UiElement` (read-only) | Selected direct child or null. |
| `Background` | `IUiBrush` | Visual behind all items. |
| `BackgroundTint` | `Color` | Background tint; default white. |

### Events

| Event | Type | Behavior |
|---|---|---|
| `SelectionChanged` | `EventHandler<UiSelectionChangedEventArgs>` | Old/new indices and items. |

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

## Type-specific XML

| Attribute | Type | Default | Meaning |
|---|---|---|---|
| `selected-index` | `int` | `-1` | Requested selection. |
| `background-tint` | `Color` | white | Selector background tint. |

## C# example

```csharp
selector.SelectionChanged += (_, args) =>
{
    Logger.Info($"Selection {args.OldIndex} -> {args.NewIndex}");
};
selector.SelectedIndex = 0;
```

## Input and focus

- Pointer press selects the direct item containing the event source.
- Direct `UiButton` items also select through their `Clicked` event.
- Directional navigation wraps around; Home selects first and End selects last.

## Runtime behavior

- Selected visual state is applied only when the direct item is a `UiControl`.
- Requested XML selection is applied as soon as enough children have been attached.

## Extending the type

- Derive a concrete selector by defining its presentation/layout policy; `UiListBox` is the current built-in implementation.

## Production pitfalls

- `ClearChildren()` clears selection without raising `SelectionChanged` in the current implementation.
- Removing the selected item raises a transition to -1; removing an earlier item adjusts the index without a selection event.
- Selection is single-only and index-based.

## See also

- [`UiItemsControl`](UiItemsControl.md)
- [`UiListBox`](UiListBox.md)
- [`UiControl`](UiControl.md)

---

_Source reviewed 2026-08-03. This page documents current implemented behavior, not a proposed API._
