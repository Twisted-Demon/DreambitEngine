# UiBrush

`IUiBrush` defines a reusable drawing strategy for a control background or
specialized surface. `UiBrush` is the abstract convenience base with
`MinimumSize`, `Parse`, `ResolveDependencies`, and `Draw`.

Brushes receive the owning element's arranged rectangle and tint. They do not
own layout children or input.

```csharp
border.Background = new SolidColorBrush();
border.BackgroundTint = Color.DarkSlateBlue;
```

In XML, assign a brush through a property element whose prefix matches the
owning tag:

```xml
<Border.Background>
  <SolidColorBrush />
</Border.Background>
```

Create a custom brush when a visual treatment will be shared by several
controls. See [Custom elements and brushes](../custom-ui.md).

