using Dreambit.ECS;
using Dreambit.Editor.Assets;
using Dreambit.Editor.Graphics;
using Dreambit.Editor.Persistence;
using Dreambit.Editor.Scenes;
using Dreambit.Editor.UI.Viewport;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Vector2 = System.Numerics.Vector2;
using XnaVector2 = Microsoft.Xna.Framework.Vector2;

namespace Dreambit.Editor.UI.Panels;

/// <summary>
/// Scene-specific viewport policy: normal scene ownership, boxed-Blueprint pick promotion,
/// hierarchy selection bounds, and Blueprint asset drops.
/// </summary>
internal sealed class ScenePanel : SceneViewportPanel
{
    private readonly SceneDocumentService _documents;
    private readonly EditorDocumentContext _documentContext;
    private readonly EditorWorkspaceState _workspace;
    private readonly EditorDragDropService _dragDrop;
    private readonly AssetDatabase _assets;
    private readonly BlueprintSourceService _blueprintSources;
    private string? _error;

    public ScenePanel(
        SceneDocumentService documents,
        EditorDocumentContext documentContext,
        EditorWorkspaceState workspace,
        SceneViewportRenderer renderer,
        EditorDragDropService dragDrop,
        AssetDatabase assets,
        BlueprintSourceService blueprintSources,
        EditorIconService icons,
        Action<string, Exception?>? reportError = null)
        : base(
            EditorPanelIds.Scene,
            "Scene",
            true,
            workspace,
            renderer,
            icons,
            reportError)
    {
        _documents = documents;
        _documentContext = documentContext;
        _workspace = workspace;
        _dragDrop = dragDrop;
        _assets = assets;
        _blueprintSources = blueprintSources;
    }

    protected override string EmptyTitle => "No scene is open";
    protected override string EmptyDetail => "Use File > New Scene or Open Scene.";
    protected override string? ViewportError => _error;
    protected override float CameraX
    {
        get => _workspace.SceneCameraX;
        set => _workspace.SceneCameraX = value;
    }
    protected override float CameraY
    {
        get => _workspace.SceneCameraY;
        set => _workspace.SceneCameraY = value;
    }
    protected override float CameraZoom
    {
        get => _workspace.SceneCameraZoom;
        set => _workspace.SceneCameraZoom = value;
    }

    protected override SceneDocument? ResolveDocument() => _documents.Current;

    protected override void ActivateDocument(SceneDocument document) =>
        _documentContext.ActivateScene();

    protected override Entity? InterpretPick(SceneDocument document, Entity? picked)
    {
        if (picked is not null &&
            document.TryGetBlueprintInstanceRoot(picked, out var boxedRoot, out _))
        {
            return boxedRoot;
        }
        return picked;
    }

    protected override RectangleF? ResolveSelectionBounds(SceneDocument document, Entity entity) =>
        document.IsBlueprintInstanceRoot(entity)
            ? SelectionOverlay.GetHierarchyDrawableBounds(entity)
            : SelectionOverlay.GetEntityDrawableBounds(entity);

    protected override void FrameDocument(SceneDocument document)
    {
        var entity = document.Selection.GetActive(document.Scene);
        if (entity is null)
            return;
        var position = entity.Transform.WorldPosition2D;
        CameraX = position.X;
        CameraY = position.Y;
    }

    protected override unsafe void DrawCanvasDropTarget(
        SceneDocument document,
        Camera2D camera,
        Vector2 mouseLocal)
    {
        if (!ImGui.BeginDragDropTarget())
            return;

        var acceptedProjectItem = false;
        try
        {
            var accepted = ImGui.AcceptDragDropPayload(EditorDragDropService.ProjectItemPayloadType);
            acceptedProjectItem = accepted.NativePtr != null;
            if (!acceptedProjectItem ||
                _dragDrop.ProjectItem is not { Kind: AssetKind.Blueprint } payload ||
                !_assets.TryGetAsset(payload.RelativePath, out var asset))
            {
                return;
            }

            using var source = _blueprintSources.Load(asset!);
            var world = camera.ScreenToWorld(new XnaVector2(mouseLocal.X, mouseLocal.Y));
            document.InstantiateBlueprint(
                source,
                new Microsoft.Xna.Framework.Vector3(world, source.Position.Z));
            _error = null;
        }
        catch (Exception exception)
        {
            _error = $"Could not instantiate Blueprint: {exception.Message}";
        }
        finally
        {
            if (acceptedProjectItem)
                _dragDrop.ClearProjectItem();
            ImGui.EndDragDropTarget();
        }
    }
}
