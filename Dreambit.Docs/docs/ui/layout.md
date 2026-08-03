# Layout, sizing, and positioning

Every element supports these common XML attributes:

| Attribute | Meaning | Default |
| --- | --- | --- |
| `x`, `y` | Offset from its anchored parent point | `0%` |
| `width`, `height` | Pixel, percentage, or `*` automatic length | `100%` |
| `anchor` | Reference point on the parent | `TopLeft` |
| `origin` | Reference point on the element | `TopLeft` |
| `z` | Sibling draw and hit-test order | `0` |
| `grid-row`, `grid-column` | Grid placement | `0` |
| `grid-row-span`, `grid-column-span` | Grid span | `1` |
| `clip-to-bounds` | Clip descendants | `false` |

Lengths are `48` pixels, `50%` of available space, or `*` for automatic/content
sizing. Anchors are the nine combinations from `TopLeft` through `BottomRight`.

Containers decide how child `x`, `y`, and size are interpreted. `Canvas` keeps
explicit placement. Stack, wrap, uniform-grid, and grid panels arrange their
children. Content controls accept only one child and add padding/alignment.

Changing layout properties in code invalidates layout automatically. For custom
state that affects desired size, call `InvalidateLayout()`.

Use `UiThickness` values as one uniform inset or four values in left, top, right,
bottom order: `padding="8"` or `padding="8,4,8,4"`.

