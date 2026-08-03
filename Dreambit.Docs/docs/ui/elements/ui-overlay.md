# UiOverlay

`UiOverlay` is a single-child modal-style surface. When visible and
`BlocksInput` is true, focus and routed input are restricted to its subtree.

```xml
<Overlay id="pause-overlay" width="100%" height="100%"
         is-visible="false" blocks-input="true"
         background-tint="#000000AA">
  <Overlay.Background><SolidColorBrush /></Overlay.Background>
  <Border width="420" height="260" anchor="Center" origin="Center" />
</Overlay>
```

Show it, then focus a control inside:

```csharp
overlay.IsVisible = true;
layout.GetRequired<UiButton>("resume").Focus();
```

An overlay is not automatically moved to the popup layer. Use `UiPopup` for a
transient surface that must render above the ordinary tree.

