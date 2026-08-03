# UiSelector

`UiSelector` is the abstract selection behavior behind `UiListBox`. It adds
`SelectedIndex`, `SelectedItem`, `SelectionChanged`, a background brush/tint,
and selection-state coordination for child controls.

```csharp
public sealed class InventorySelector : UiSelector
{
    // Add game-specific navigation or item generation here.
}
```

In XML, derived selectors use `selected-index` and `background-tint`, plus a
`Selector.Background` property element when the concrete XML tag is
`<Selector>`. Selection is corrected as children are added, removed, or cleared.

Derive from this type when you need selectable visual children but a different
presentation from the built-in list box.

