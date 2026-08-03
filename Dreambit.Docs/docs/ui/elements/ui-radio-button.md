# UiRadioButton

`UiRadioButton` is a checkbox that enforces one selected item per `GroupName`
within its layout.

```xml
<RadioButton id="easy" group="difficulty" width="220" height="28"
             is-checked="true" mark-tint="#8FC7FFFF">
  <Text text="Easy" font="monogram" font-size="18" />
</RadioButton>
<RadioButton id="hard" group="difficulty" width="220" height="28">
  <Text text="Hard" font="monogram" font-size="18" />
</RadioButton>
```

Subscribe to inherited `CheckedChanged` and act when its Boolean argument is
true. Controls with different or empty group names do not participate in the
same exclusive set.

