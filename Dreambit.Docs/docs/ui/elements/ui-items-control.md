# UiItemsControl

`UiItemsControl` is a stack panel with item-oriented methods. It exposes
`Items`, `AddItem`, `RemoveItem`, `ClearItems`, and `SetItems`.

```csharp
var list = new UiItemsControl();
list.SetItems(scores, score => new UiText
{
    Text = score.ToString(),
    Height = UiLength.Pixels(28)
});
```

The XML tag `<ItemsControl>` accepts ordinary visual children and inherited
stack-panel layout attributes. It does not select items; use `UiListBox` for
selection.

This class is most useful as a base for custom data-driven controls. Dreambit
does not retain a data binding layer, so rebuild or update visual items when the
source collection changes.

