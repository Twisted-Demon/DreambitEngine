# UiScrollBar

`UiScrollBar` specializes `UiSlider` with viewport information and a proportional
minimum-sized thumb.

```xml
<ScrollBar id="scroll" width="260" height="20"
           minimum="0" maximum="100" value="20" step="5"
           viewport-size="30" large-change="10"
           minimum-thumb-size="12" />
```

`ViewportSize` represents the visible amount in the same units as the range.
`LargeChange` is available for page-style interactions. `MinimumThumbSize`
prevents the thumb from becoming unusably small.

The current element exposes the scroll value but is not automatically connected
to a scroll-view container. Bind `ValueChanged` to the content offset in your
own scrolling surface.

