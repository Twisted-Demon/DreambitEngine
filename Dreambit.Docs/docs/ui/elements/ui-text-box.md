# UiTextBox

`UiTextBox` is a focusable single-line text editor with selection, clipboard
shortcuts, placeholder text, password masking, and caret drawing.

```xml
<TextBox id="name" width="300" height="38" padding="8"
         placeholder="Pilot name" font="monogram" font-size="18"
         max-length="24" text-color="#FFFFFFFF"
         placeholder-color="#89909EFF"
         selection-color="#3269AACC" caret-color="#FFFFFFFF" />
```

```csharp
var box = layout.GetRequired<UiTextBox>("name");
box.TextChanged += (_, text) => ValidateName(text);
box.SelectAll();
```

Set `password-character="*"` to mask display. `CaretIndex`, `SelectionStart`,
and `SelectionLength` are read-only; call `Select(start, length)` to change the
selection. Ctrl+A/C/X/V use the UI clipboard abstraction.

