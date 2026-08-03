# UiRangeBase

`UiRangeBase` is the abstract shared behavior for sliders, scroll bars, and
progress bars. It owns `Minimum`, `Maximum`, `Value`, `Step`, `NormalizedValue`,
and `ValueChanged`.

```csharp
UiRangeBase range = layout.GetRequired<UiSlider>("volume");
range.Minimum = 0;
range.Maximum = 100;
range.Step = 5;
range.ValueChanged += (_, value) => SetVolume(value / 100f);
range.SetNormalizedValue(0.5f);
```

`Value` is clamped to the range and quantized by `Step` when step is positive.
XML attributes are `minimum`, `maximum`, `value`, and `step`.

Derive from this base for a custom range visualization or interaction model.

