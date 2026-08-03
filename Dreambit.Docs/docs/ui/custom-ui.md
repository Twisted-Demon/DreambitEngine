# Custom elements and brushes

Custom UI types in any loaded assembly are discovered automatically. A concrete
element needs a public parameterless constructor and must derive from
`UiElement`. Its default XML tag drops a leading `Ui`.

```csharp
[UiXmlName("HealthPips")]
public sealed class HealthPips : UiElement
{
    public int Count { get; set; } = 3;

    public override void Parse(XmlNode node)
    {
        Count = UiXmlParser.ParseInt(node, "count", 3);
    }

    public override void OnDraw()
    {
        // Draw through Graphics.SpriteBatch.
    }
}
```

Override measurement/arrangement when desired size or child layout differs from
the base implementation. Call `InvalidateLayout` when a property changes size
and `InvalidateDependencies` when an asset path changes.

Custom brushes implement `IUiBrush` or derive from `UiBrush`, parse their XML,
resolve dependencies, and draw into supplied bounds. Give a custom XML name with
`[UiXmlName]` when convention would collide with another loaded type. Ambiguous
tag names are rejected with the matching assembly-qualified types.

