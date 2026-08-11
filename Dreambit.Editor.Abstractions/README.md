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
        context.DrawDefaultInspector();
    }
}
```

Use `RecordChange` for mutations that should participate in Undo/Redo. Exceptions from a
custom inspector are isolated and reported in the Editor Console.
