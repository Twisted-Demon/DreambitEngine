# UiListBox

`UiListBox` is a selectable vertical items control.

```xml
<ListBox id="loadout" width="260" height="150"
         spacing="4" selected-index="0"
         background-tint="#11141AFF">
  <ListBox.Background><SolidColorBrush /></ListBox.Background>
  <Button width="100%" height="32"><Text text="Scout" /></Button>
  <Button width="100%" height="32"><Text text="Tank" /></Button>
</ListBox>
```

```csharp
var list = layout.GetRequired<UiListBox>("loadout");
list.SelectionChanged += (_, args) =>
    Logger.Info("Selected {0}", args.NewIndex);
```

The selected child receives `IsSelected`, so its `selected-tint` is used when it
is a `UiControl`. Read `SelectedItem` or `SelectedIndex` in code.

