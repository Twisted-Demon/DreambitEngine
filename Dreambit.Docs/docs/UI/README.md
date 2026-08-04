# Dreambit UI Element and Brush Reference

Production-level replacement documentation for every current Dreambit UI element and brush/interface type.

The API and implementation behavior were reviewed against `Twisted-Demon/DreambitEngine` on 2026-08-03. The replacement set preserves the existing folder and file names and targets the UI implementation represented by:

`ef6e5b9c600ad6e215c53ea287a0c7858884ce00`  
`more ui elements, fixed ui rendering`

## What each page now includes

- Inheritance and XML availability.
- Declared properties, events, and methods.
- Common and type-specific XML attributes with defaults.
- Complete XML and C# examples.
- Measurement, arrangement, ownership, input, focus, and dependency behavior.
- Runtime mutation guidance.
- Performance characteristics and draw-call implications.
- Production pitfalls based on current implementation details.
- Source file location and related-type links.

## Elements

- [UiElement](Elements/UiElement.md)
- [UiContainer](Elements/UiContainer.md)
- [UiPanel](Elements/UiPanel.md)
- [UiCanvas](Elements/UiCanvas.md)
- [UiContentControl](Elements/UiContentControl.md)
- [UiControl](Elements/UiControl.md)
- [UiBorder](Elements/UiBorder.md)
- [UiButton](Elements/UiButton.md)
- [UiToggleButton](Elements/UiToggleButton.md)
- [UiCheckBox](Elements/UiCheckBox.md)
- [UiRadioButton](Elements/UiRadioButton.md)
- [UiText](Elements/UiText.md)
- [UiTextBox](Elements/UiTextBox.md)
- [UiTexture](Elements/UiTexture.md)
- [UiSpacer](Elements/UiSpacer.md)
- [UiGrid](Elements/UiGrid.md)
- [UiStackPanelBase](Elements/UiStackPanelBase.md)
- [UiVerticalStackPanel](Elements/UiVerticalStackPanel.md)
- [UiHorizontalStackPanel](Elements/UiHorizontalStackPanel.md)
- [UiStackPanel](Elements/UiStackPanel.md)
- [UiUniformGrid](Elements/UiUniformGrid.md)
- [UiWrapPanel](Elements/UiWrapPanel.md)
- [UiViewbox](Elements/UiViewbox.md)
- [UiItemsControl](Elements/UiItemsControl.md)
- [UiSelector](Elements/UiSelector.md)
- [UiListBox](Elements/UiListBox.md)
- [UiRangeBase](Elements/UiRangeBase.md)
- [UiSlider](Elements/UiSlider.md)
- [UiScrollBar](Elements/UiScrollBar.md)
- [UiProgressBar](Elements/UiProgressBar.md)
- [UiComboBox](Elements/UiComboBox.md)
- [UiPopup](Elements/UiPopup.md)
- [UiTooltip](Elements/UiTooltip.md)
- [UiOverlay](Elements/UiOverlay.md)

## Brushes

- [IUiBrush](Brushes/IUiBrush.md)
- [UiBrush](Brushes/UiBrush.md)
- [SolidColorBrush](Brushes/SolidColorBrush.md)
- [SpriteBrush](Brushes/SpriteBrush.md)
- [NineSliceBrush](Brushes/NineSliceBrush.md)
- [TiledSpriteBrush](Brushes/TiledSpriteBrush.md)
- [OutlineBrush](Brushes/OutlineBrush.md)
- [LayeredBrush](Brushes/LayeredBrush.md)

## Core conventions

- Concrete `Ui...` element names become XML tags by removing the `Ui` prefix.
- Brush class names are used as XML element names.
- Element dimensions use `*` for automatic content size.
- Grid track definitions use `*` for weighted remaining space.
- Colors accept `#RRGGBB` and `#RRGGBBAA`.
- Thickness accepts one integer or `left,top,right,bottom`.
- IDs are case-sensitive.
- XML element width and height default to `100%`; C#-constructed base elements default to zero pixels unless a constructor overrides them.
- Brush properties use property-element syntax, such as `<Button.Background>...</Button.Background>`.

## Replacement

Replace the existing reference folder with this folder as a unit so links remain consistent. The package contains 34 element pages, 8 brush pages, and this README.
