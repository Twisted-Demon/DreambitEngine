# `UiElement`

Base class for every retained Dreambit UI node.

**Status:** Abstract base type  
**Namespace:** `Dreambit.UI`  
**Source:** `DreambitEngine/UI/Elements/UiElement.cs`  
**Validated against:** DreambitEngine `main` / `ef6e5b9c600ad6e215c53ea287a0c7858884ce00`

## Inheritance

`UiElement`

## When to use

Use this page as the canonical reference for geometry, lifecycle, routed input, focus, pointer capture, invalidation, and XML attributes inherited by every UI element.

## Declared API

### Properties and fields

| Member | Type | Behavior |
|---|---|---|
| `Id` | `string` field | Optional, case-sensitive layout lookup ID. |
| `Parent` | `UiContainer` field | Owning container; managed by container attachment APIs. |
| `IsVisible` | `bool` | Whether this element and its subtree participate in layout, drawing, and input. |
| `IsEnabled` | `bool` | Whether this element and its subtree can receive input. |
| `IsHitTestVisible` | `bool` | Whether this element can be the direct pointer target. |
| `IsFocusable` | `bool` | Whether focus navigation can move to this element. |
| `CapturesKeyboardInput` | `bool` | Whether focused ownership consumes keyboard input globally. |
| `ClipToBounds` | `bool` | Whether descendants are clipped to this element's arranged bounds. |
| `Tooltip` | `UiTooltip` | Delayed popup owned by this element. |
| `IsFocused` | `bool` (read-only) | Current keyboard/controller focus state. |
| `IsPointerOver` | `bool` (read-only) | True when the pointer route includes this element. |
| `X`, `Y` | `UiLength` | Offsets resolved against the parent. |
| `Width`, `Height` | `UiLength` | Pixel, percentage, or automatic dimensions. |
| `Anchor`, `Origin` | `UiAnchor` | Parent and local reference points used for positioning. |
| `ZIndex` | `int` | Sibling draw and hit-test ordering. |
| `GridRow`, `GridColumn` | `int` | Grid placement, clamped to non-negative values. |
| `GridRowSpan`, `GridColumnSpan` | `int` | Grid spans, clamped to at least one. |
| `Bounds` | `Rectangle` field | Final arranged rectangle. |
| `DesiredSize` | `Point` (read-only) | Most recent measurement result. |
| `Children` | `List<UiElement>` field | Child list. Prefer container methods over direct mutation. |
| `IsEffectivelyVisible` | `bool` | Visibility including ancestors. |
| `IsEffectivelyEnabled` | `bool` | Enabled state including ancestors. |

### Events

| Event | Type | Behavior |
|---|---|---|
| `PointerPressed`, `PointerReleased`, `PointerMoved`, `PointerWheelChanged` | event | Routed pointer events that bubble from source toward ancestors. |
| `KeyPressed`, `KeyReleased` | event | Routed key transitions on the focus route. |
| `NavigationRequested` | event | Directional navigation before default focus movement. |
| `Activated`, `Cancelled` | event | Abstract UI commands such as Enter/A and Escape/B. |
| `GotFocus`, `LostFocus` | event | Focus transitions. |

### Methods

| Member | Behavior |
|---|---|
| `InvalidateLayout()` | Invalidates this element and recursively invalidates descendants. |
| `InvalidateDependencies()` | Requests asset re-resolution for this element. |
| `Focus()` | Attempts to move layout focus to this element. |
| `CapturePointer()` | Captures subsequent pointer events to this element. |
| `ReleasePointerCapture()` | Releases capture when owned by this element. |
| `Measure(Point)` | Calculates `DesiredSize` under an available-size constraint. |
| `Arrange(Rectangle)` | Assigns `Bounds` and arranges descendants. |
| `Update(in UiInputState)` | Runs retained per-frame behavior when effectively visible and enabled. |
| `OnDraw()` | Draw hook executed before children. |
| `Parse(XmlNode)` | Derived XML parser called after common attributes are parsed. |
| `ResolveDependencies()` | Derived asset-resolution hook. |

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

## XML example

```xml
<Panel id="status-panel"
       x="24"
       y="24"
       width="320"
       height="*"
       anchor="TopLeft"
       origin="TopLeft"
       z="10"
       is-visible="true"
       is-enabled="true"
       clip-to-bounds="false" />
```

## C# example

```csharp
using Dreambit.UI;
using Microsoft.Xna.Framework;

UiElement element = layout.GetRequired<UiElement>("status-panel");

element.IsVisible = true;
element.ZIndex = 20;
element.GotFocus += (_, _) => Logger.Info("UI focus entered status-panel");

bool focused = element.Focus();
```

## Layout behavior

- Measurement is two-pass: fixed/percentage dimensions resolve directly; `*` uses `MeasureContent`.
- `Anchor` chooses a point on the parent and `Origin` chooses the point on the element placed there.
- Invisible elements measure to `Point.Zero` and arrange to `Rectangle.Empty`.
- Children draw by ascending `ZIndex`; equal values preserve insertion order. Hit testing runs in reverse visual order.

## Input and focus

- Events bubble from the source through its ancestors. Set `args.Handled = true` to stop normal downstream behavior.
- Pointer capture is required for robust press and drag gestures that continue outside original bounds.
- Focus, capture, and pointer state are revalidated when visibility, enabled state, focusability, parentage, or layout changes.

## Ownership and lifecycle

- `Parent`, layout attachment, ID validation, and interaction cleanup are container/layout responsibilities.
- Do not insert directly into `Children`; use `UiContainer.AddChild`, `RemoveChild`, or `ClearChildren`.
- Replacing a `UiLayout` invalidates all references to elements from the previous layout.

## Extending the type

- Override `MeasureContent(Point)` to support automatic dimensions.
- Override `Arrange(Rectangle)` only when the element owns a distinct child-layout policy.
- Call `InvalidateLayout()` when a property changes desired size or child placement.
- Call `InvalidateDependencies()` when a property changes an external asset.
- Use protected routed-input hooks rather than polling global input inside the element.

## Performance notes

- Prefer retained mutation (`Text`, `Value`, `IsVisible`) over rebuilding layouts.
- Invalidate dependencies only when an asset-backed property changes.
- Keep event handlers unsubscribed when scene-owned objects outlive or are replaced independently of the layout.

## Production pitfalls

- XML defaults `width` and `height` to `100%`, but a newly constructed C# element begins at zero pixels unless a constructor overrides it. Set dimensions explicitly in programmatic UI.
- `InvalidateLayout()` currently recurses through the entire subtree. Avoid changing layout-affecting properties every frame on large trees.
- `ClipToBounds` changes GPU scissor state and can flush/restart the deferred UI sprite batch. Use it only where clipping is required.
- `ZIndex` sorting currently uses LINQ during drawing; avoid deep containers with frequently rendered large child counts until profiling justifies optimization.

## See also

- [`UiContainer`](UiContainer.md)
- [`UiTooltip`](UiTooltip.md)

---

_Source reviewed 2026-08-03. This page documents current implemented behavior, not a proposed API._
