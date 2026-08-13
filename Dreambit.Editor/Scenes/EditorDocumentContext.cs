using Dreambit.Editor.Assets;
using Dreambit.Editor.Undo;

namespace Dreambit.Editor.Scenes;

internal enum EditorDocumentKind
{
    Scene,
    Blueprint,
    Asset
}

/// <summary>
/// Records the document the user most recently focused. Visibility and panel draw order must
/// never decide where global save/undo commands or shared document panels are routed.
/// </summary>
internal sealed class EditorDocumentContext(
    SceneDocumentService scenes,
    BlueprintEditingService blueprints,
    AssetEditingService assets)
{
    public EditorDocumentKind ActiveKind { get; private set; } = EditorDocumentKind.Scene;

    /// <summary>
    /// Scene-shaped document shown by Hierarchy. Generic asset focus leaves the normal scene
    /// available for later scene focus; Blueprint focus explicitly substitutes the authored
    /// Blueprint document and never falls through while that preview is unavailable.
    /// </summary>
    public SceneDocument? Current => ActiveKind switch
    {
        // A Blueprint preview can be temporarily unavailable while its assembly or
        // content is rebuilding. Falling through to the normal scene in that state
        // would make Hierarchy and Inspector edit a different document than the one
        // selected by global save/undo routing.
        EditorDocumentKind.Blueprint => blueprints.Current,
        _ => scenes.Current
    };
    public SelectionService Selection => IsBlueprint
        ? blueprints.Selection
        : scenes.Selection;
    public bool IsBlueprint => ActiveKind == EditorDocumentKind.Blueprint;
    public bool IsAsset => ActiveKind == EditorDocumentKind.Asset;
    public BlueprintEditingService Blueprints => blueprints;
    public DreambitAssetDocument? AssetDocument =>
        ActiveKind is EditorDocumentKind.Asset or EditorDocumentKind.Blueprint
            ? assets.Current
            : null;
    public UndoService? Undo => ActiveKind switch
    {
        EditorDocumentKind.Scene => scenes.Current?.Undo,
        // The asset document owns Blueprint history. Its preview SceneDocument may be
        // suspended independently during reload or after a preview construction failure.
        EditorDocumentKind.Blueprint => assets.Current?.Undo,
        EditorDocumentKind.Asset => assets.Current?.Undo,
        _ => null
    };

    public void ActivateScene() => ActiveKind = EditorDocumentKind.Scene;

    public void ActivateBlueprint() => ActiveKind = EditorDocumentKind.Blueprint;

    public void ActivateAsset()
    {
        if (assets.Selected is not null)
            ActiveKind = EditorDocumentKind.Asset;
    }

    public void Activate(SceneDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        ActiveKind = ReferenceEquals(document, blueprints.Current)
            ? EditorDocumentKind.Blueprint
            : EditorDocumentKind.Scene;
    }
}
