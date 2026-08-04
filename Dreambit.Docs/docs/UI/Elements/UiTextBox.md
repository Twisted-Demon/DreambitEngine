# `UiTextBox`

Single-line editable text control with caret, selection, password display, and internal clipboard shortcuts.

**Status:** XML tag: `TextBox`  
**Namespace:** `Dreambit.UI`  
**Source:** `DreambitEngine/UI/Elements/UiTextBox.cs`  
**Validated against:** DreambitEngine `main` / `ef6e5b9c600ad6e215c53ea287a0c7858884ce00`

## Inheritance

`UiElement` → `UiContainer` → `UiContentControl` → `UiControl` → `UiTextBox`

## Declared API

### Properties and fields

| Member | Type | Behavior |
|---|---|---|
| `Text` | `string` | Edited value; constrained by `MaxLength`. |
| `Placeholder` | `string` | Shown only while empty and unfocused. |
| `TextColor`, `PlaceholderColor` | `Color` | Normal and placeholder colors. |
| `SelectionColor`, `CaretColor` | `Color` | Selection and caret visuals. |
| `FontPath` | `string` | Font resource path; default `monogram`. |
| `FontSize` | `float` | Font size, clamped to at least one; default 18. |
| `MaxLength` | `int` | Maximum characters; zero means unlimited. |
| `PasswordCharacter` | `char?` | Display mask; null means normal display. |
| `CaretIndex` | `int` (read-only) | Current insertion index. |
| `SelectionStart`, `SelectionLength` | `int` (read-only) | Current selection. |

### Events

| Event | Type | Behavior |
|---|---|---|
| `TextChanged` | `Action<UiTextBox, string>` | Raised for user edits and programmatic changes. |

### Methods

| Member | Behavior |
|---|---|
| `Select(int start, int length)` | Selects a clamped range. |
| `SelectAll()` | Selects the complete value. |

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

## Type-specific XML

| Attribute | Type | Default | Meaning |
|---|---|---|---|
| `text` | `string` | empty | Initial text. |
| `placeholder` | `string` | empty | Placeholder. |
| `font` | `string` | `monogram` | Font path. |
| `font-size` | `float` | `18` | Font size. |
| `max-length` | `int` | `0` | Zero means unlimited. |
| `password-character` | `string` | empty | Only the first character is used. |
| `text-color` | `Color` | white | Text color. |
| `placeholder-color` | `Color` | `150,150,160` | Placeholder color. |
| `selection-color` | `Color` | `50,105,170,180` | Selection color. |
| `caret-color` | `Color` | white | Caret color. |

## XML example

```xml
<TextBox id="player-name"
         width="280" height="38"
         placeholder="Commander name"
         font="monogram" font-size="20"
         max-length="24"
         background-tint="#1B2230"
         focused-tint="#273A54">
    <TextBox.Background>
        <OutlineBrush thickness="2" />
    </TextBox.Background>
</TextBox>
```

## C# example

```csharp
UiTextBox nameBox = layout.GetRequired<UiTextBox>("player-name");
nameBox.TextChanged += (_, value) => profile.DisplayName = value.Trim();

nameBox.Text = profile.DisplayName;
nameBox.SelectAll();
nameBox.Focus();
```

## Input and focus

- Focusable, hit-test visible, keyboard-capturing, and clipped by default.
- Pointer drag selects text using pointer capture.
- Supports Left/Right, Home/End, Backspace/Delete, Shift selection, Ctrl+A/C/X/V, and text-input characters.
- Horizontal scrolling keeps the caret visible.

## Production pitfalls

- The control is single-line. Newline/control characters are ignored.
- `UiClipboard` is an engine-local clipboard abstraction; the current system does not automatically synchronize with the operating-system clipboard.
- Password masking changes display only; the underlying `Text` remains plain in memory.
- `TextChanged` fires for programmatic assignment and max-length truncation.

## See also

- [`UiText`](UiText.md)
- [`UiControl`](UiControl.md)

---

_Source reviewed 2026-08-03. This page documents current implemented behavior, not a proposed API._
