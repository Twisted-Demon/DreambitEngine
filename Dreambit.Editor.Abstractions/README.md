# Dreambit.Editor.Abstractions

Public contracts for game-defined Dreambit Editor inspectors. Game projects reference this
package; Dreambit.Editor discovers implementations from the game's collectible assembly at
runtime, so the Editor never needs a compile-time dependency on the game.

```csharp
using Dreambit.EditorApi;

[DreambitCustomEditor(typeof(MyComponent))]
public sealed class MyComponentEditor : IDreambitCustomEditor
{
    public void Draw(IEditorInspectorContext context)
    {
        var component = (MyComponent)context.ActiveTarget!;

        using var section = EditorGui.Section(
            "MyComponent.Settings",
            "Movement Settings");
        if (!section.IsOpen)
            return;

        var speed = component.Speed;
        if (EditorGui.Property(
                "MyComponent.Speed",
                "Speed",
                ref speed,
                speed: 0.05f,
                min: 0f,
                tooltip: "Maximum movement speed."))
        {
            context.RecordChange("Change Speed", () =>
            {
                foreach (var target in context.Targets.Cast<MyComponent>())
                    target.Speed = speed;
            });
        }
    }
}
```

`EditorGui` provides Dreambit's stable-ID property layout, typed fields, sections, messages,
buttons, search, references, and balanced ID/disabled scopes. Every control takes an explicit
ID so repeated labels remain safe within the same Inspector.

Use `RecordChange` for mutations that should participate in Undo/Redo and serialization. Do not
mutate `ActiveTarget` directly in response to a control. Exceptions from a custom inspector are
isolated and reported in the Editor Console.
