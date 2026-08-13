using Dreambit.Editor.Assets;
using Dreambit.Editor.Graphics;
using Dreambit.Editor.Persistence;
using Dreambit.Editor.Scenes;
using Dreambit.Editor.UI.Viewport;
using ImGuiNET;

namespace Dreambit.Editor.UI.Panels;

/// <summary>
/// Blueprint-specific viewport policy. Picks remain exact so authored children are directly
/// selectable; Blueprint source/preview lifecycle stays in BlueprintEditingService.
/// </summary>
internal sealed class BlueprintViewPanel : SceneViewportPanel
{
    private readonly AssetDatabase _assets;
    private readonly AssetEditingService _assetEditing;
    private readonly BlueprintEditingService _blueprints;
    private readonly EditorDocumentContext _documentContext;
    private readonly EditorWorkspaceState _workspace;
    private AssetRecord? _asset;
    private long _observedAssetVersion = -1;
    private DateTimeOffset _sourceWriteUtc;
    private bool _needsRebuild;
    private string? _error;

    public BlueprintViewPanel(
        AssetDatabase assets,
        AssetEditingService assetEditing,
        BlueprintEditingService blueprints,
        EditorDocumentContext documentContext,
        EditorWorkspaceState workspace,
        SceneViewportRenderer renderer,
        EditorIconService icons,
        Action<string, Exception?>? reportError = null)
        : base(
            EditorPanelIds.Blueprint,
            "Blueprint View",
            false,
            workspace,
            renderer,
            icons,
            reportError)
    {
        _assets = assets;
        _assetEditing = assetEditing;
        _blueprints = blueprints;
        _documentContext = documentContext;
        _workspace = workspace;
        _assetEditing.Changed += OnAssetDocumentChanged;
        _assetEditing.PreviewChanged += OnAssetPreviewChanged;

        if (!string.IsNullOrWhiteSpace(workspace.LastBlueprintPath) &&
            assets.TryGetAsset(workspace.LastBlueprintPath, out var restored) &&
            restored!.Kind == AssetKind.Blueprint)
        {
            _asset = restored;
            _needsRebuild = true;
        }
    }

    protected override string EmptyTitle =>
        _asset is null ? "No Blueprint is open" : "Blueprint preview unavailable";
    protected override string EmptyDetail =>
        _asset is null
            ? _error ?? "Double-click a Blueprint in Project."
            : _blueprints.Error ?? _error ?? "The preview will return after game code reloads.";
    protected override string? ViewportError => _blueprints.Error ?? _error;
    protected override float CameraX
    {
        get => _workspace.BlueprintCameraX;
        set => _workspace.BlueprintCameraX = value;
    }
    protected override float CameraY
    {
        get => _workspace.BlueprintCameraY;
        set => _workspace.BlueprintCameraY = value;
    }
    protected override float CameraZoom
    {
        get => _workspace.BlueprintCameraZoom;
        set => _workspace.BlueprintCameraZoom = value;
    }

    public void Open(AssetRecord asset)
    {
        ArgumentNullException.ThrowIfNull(asset);
        if (asset.Kind != AssetKind.Blueprint)
            return;
        if (!_assetEditing.Select(asset))
        {
            _error = $"Could not open '{asset.RelativePath}'. The current asset remains open.";
            return;
        }
        _asset = asset;
        _observedAssetVersion = -1;
        _workspace.LastBlueprintPath = asset.RelativePath;
        IsOpen = true;
        _needsRebuild = true;
        // Opening a Blueprint is a document-context action even when its preview
        // cannot be constructed until a pending assembly reload finishes.
        _documentContext.ActivateBlueprint();
    }

    protected override void BeforeDocumentResolution()
    {
        RefreshAssetRecord();
        var focused = ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows);
        if (_asset is not null && focused)
        {
            // Preview construction and document focus are independent. In particular,
            // focusing an unavailable preview must not expose the normal scene through
            // the shared Hierarchy and Inspector.
            _documentContext.ActivateBlueprint();
            if (_blueprints.Current is null &&
                _assetEditing.Current?.Asset.Id != _asset.Id)
            {
                _needsRebuild = true;
            }
        }
        if (_needsRebuild && _blueprints.Current is not null &&
            _assetEditing.Current?.Asset.Id == _asset?.Id)
        {
            // The service may already have rebuilt synchronously from the same asset
            // notification. Avoid replacing that fresh preview a second time.
            _needsRebuild = false;
        }
        // A background/restored Blueprint window must not steal the single asset
        // document from a Scene or generic asset the user currently owns. Explicit
        // Open() activates Blueprint context; reload recovery keeps that context too.
        if (_needsRebuild && (focused || _documentContext.IsBlueprint))
            RebuildPreview();
    }

    protected override SceneDocument? ResolveDocument() => _blueprints.Current;

    protected override void ActivateDocument(SceneDocument document) =>
        _documentContext.ActivateBlueprint();

    protected override void PrepareScene(SceneDocument document, EditorScene scene) =>
        scene.EditorTick();

    protected override void FrameDocument(SceneDocument document)
    {
        var entity = document.Selection.GetActive(document.Scene) ?? _blueprints.Root;
        if (entity is null)
            return;
        CameraX = entity.Transform.WorldPosition.X;
        CameraY = entity.Transform.WorldPosition.Y;
    }

    protected override void DrawToolbarSuffix(SceneDocument? document)
    {
        ImGui.SameLine();
        ImGui.TextDisabled(_asset?.RelativePath ?? "Blueprint");
    }

    private void RefreshAssetRecord()
    {
        if (_asset is null)
            return;
        var snapshot = _assets.GetSnapshot();
        if (snapshot.Version == _observedAssetVersion)
            return;
        _observedAssetVersion = snapshot.Version;
        var current = snapshot.Assets.FirstOrDefault(asset => asset.Id == _asset.Id);
        if (current is null)
        {
            _asset = null;
            _needsRebuild = false;
            _workspace.LastBlueprintPath = string.Empty;
            _error = "The open Blueprint was removed from the project.";
            return;
        }
        if (current.LastWriteUtc != _sourceWriteUtc &&
            _assetEditing.Current?.Asset.Id != current.Id)
        {
            _needsRebuild = true;
        }
        _asset = current;
        // The database preserves identity across moves and renames. Persist its
        // current path rather than leaving the workspace pointed at the old name.
        _workspace.LastBlueprintPath = current.RelativePath;
    }

    private void RebuildPreview()
    {
        _needsRebuild = false;
        if (_asset is null)
            return;
        try
        {
            if (!_blueprints.Open(_asset))
            {
                _error = $"Could not open '{_asset.RelativePath}'. The current asset remains open.";
                return;
            }
            _sourceWriteUtc = _asset.LastWriteUtc;
            _error = null;
        }
        catch (Exception exception)
        {
            _error = exception.Message;
        }
    }

    private void OnAssetDocumentChanged()
    {
        // This notification also covers removal of the currently open asset while
        // the panel is hidden, so keep its persisted identity from going stale.
        RefreshAssetRecord();
        if (_assetEditing.Current?.Asset.Id == _asset?.Id)
            _needsRebuild = true;
    }

    private void OnAssetPreviewChanged(DreambitAssetDocument document)
    {
        // BeforeDocumentResolution distinguishes a synchronous service rebuild from a
        // failed/suspended preview, independent of event-subscriber ordering.
        if (document.Asset.Id == _asset?.Id)
            _needsRebuild = true;
    }

    protected override void DisposeViewport()
    {
        // Asset database rename notifications do not reopen/reselect the asset document.
        // Resolve the stable asset ID one final time so a hidden panel also persists the
        // latest path (or clears a Blueprint that was removed) during shutdown.
        RefreshAssetRecord();
        _assetEditing.Changed -= OnAssetDocumentChanged;
        _assetEditing.PreviewChanged -= OnAssetPreviewChanged;
    }
}
