# UiControl

`UiControl` extends a content control with focus, visual state, templates, and
state-specific tints. It is the base of buttons, range controls, popups, text
boxes, and combo boxes.

```xml
<Control width="200" height="44"
         background-tint="#25303CFF"
         hover-tint="#34475BFF"
         pressed-tint="#18222CFF"
         focused-tint="#456F9AFF"
         disabled-tint="#77777788" />
```

Other state tints include `checked-tint` and `selected-tint`. `VisualState`
combines normal, pointer, focus, disabled, checked, and selection state. Set a
`Template` in code and call `ApplyTemplate` when building a reusable custom
control shell.

