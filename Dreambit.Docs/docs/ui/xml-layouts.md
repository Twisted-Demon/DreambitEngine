# XML layouts

A layout has one `<Ui>` document root and one or more visual children:

```xml
<Ui>
  <Border width="100%" height="100%"
          padding="24" background-tint="#18202AFF">
    <Border.Background>
      <SolidColorBrush />
    </Border.Background>

    <VerticalStackPanel width="100%" height="100%" spacing="12">
      <Text id="title" width="100%" height="48"
            text="Dreambit" font="monogram" font-size="32" />
      <Button id="play-button" width="240" height="44">
        <Text text="Play" font="monogram" font-size="20" />
      </Button>
    </VerticalStackPanel>
  </Border>
</Ui>
```

Element tag names drop the `Ui` prefix (`UiButton` becomes `<Button>`). Brush
tags keep their class name (`<SolidColorBrush>`). Names are case-sensitive.

Property elements use `ElementTag.PropertyName` and contain exactly one value.
They are used for brushes, content-like properties, and tooltips. Ordinary
visual children are added to the element's container.

Load a file through [`UiFrame`](../ecs/components/ui-frame.md). `UiLoader` can
also parse a complete XML string for tests or tools.

Colors accept `#RRGGBB` or `#RRGGBBAA`. Numeric parsing uses invariant culture.
Duplicate IDs are rejected.

