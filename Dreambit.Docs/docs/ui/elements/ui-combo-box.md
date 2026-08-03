# UiComboBox

`UiComboBox` displays one selected string and opens a popup list.

```xml
<ComboBox id="resolution" width="240" height="36"
          items="1280x720,1600x900,1920x1080"
          selected-index="0" item-height="28"
          font="monogram" font-size="18"
          text-color="#FFFFFFFF" popup-tint="#242832FF" />
```

```csharp
var combo = layout.GetRequired<UiComboBox>("resolution");
combo.SelectionChanged += (_, index, value) => ApplyResolution(value);
combo.SetItems(["Windowed", "Fullscreen"]);
```

Use `OpenDropDown` and `CloseDropDown` when another control drives it.
`SelectedIndex`, `SelectedItem`, and `IsDropDownOpen` expose state. Items are
plain strings; build a custom popup/list control for rich item templates.

