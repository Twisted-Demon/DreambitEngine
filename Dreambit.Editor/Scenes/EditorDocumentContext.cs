namespace Dreambit.Editor.Scenes;

internal enum EditorDocumentKind
{
    Scene,
    Blueprint
}

/// <summary>Tracks which docked viewport owns the shared Hierarchy and Inspector.</summary>
internal sealed class EditorDocumentContext(
    SceneDocumentService scenes,
    BlueprintEditingService blueprints)
{
    public EditorDocumentKind ActiveKind { get; private set; } = EditorDocumentKind.Scene;
    public SceneDocument? Current =>
        ActiveKind == EditorDocumentKind.Blueprint && blueprints.Current is not null
            ? blueprints.Current
            : scenes.Current;
    public SelectionService Selection => Current?.Selection ?? scenes.Selection;
    public bool IsBlueprint => ActiveKind == EditorDocumentKind.Blueprint && blueprints.Current is not null;
    public BlueprintEditingService Blueprints => blueprints;

    public void ActivateScene() => ActiveKind = EditorDocumentKind.Scene;
    public void ActivateBlueprint() => ActiveKind = EditorDocumentKind.Blueprint;
}
