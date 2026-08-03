# UiPopup

`UiPopup` is a topmost single-content control placed relative to another
element or the viewport.

```xml
<Popup id="context-menu" width="220" height="160"
       placement-target="more-button" placement="Bottom"
       horizontal-offset="0" vertical-offset="6"
       stays-open="false" is-open="false">
  <VerticalStackPanel><!-- commands --></VerticalStackPanel>
</Popup>
```

```csharp
var popup = layout.GetRequired<UiPopup>("context-menu");
popup.Open();
popup.Close();
```

Placement options are `Bottom`, `Top`, `Left`, `Right`, `Center`, and
`Absolute`. Set `PlacementTarget` in code or `placement-target` by ID in XML.
Absolute placement uses the popup's own `x` and `y` values.
Dismissible popups close when the pointer targets something outside; set
`StaysOpen` when closure is fully controlled by code.
