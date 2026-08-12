using Dreambit.ECS;
using Dreambit.Editor.Assets;
using Dreambit.Editor.Compilation;

namespace Dreambit.Editor.Scenes;

/// <summary>
/// Presents an EntityBlueprint as a one-root SceneDocument. This lets every
/// hierarchy and inspector operation use the same structural/lifecycle path as
/// normal scene editing while snapshots are written back to the asset document.
/// </summary>
internal sealed class BlueprintEditingService : IDisposable
{
    private readonly AssetDatabase _assets;
    private readonly AssetEditingService _assetEditing;
    private readonly GameAssemblyLoadService _assemblies;
    private readonly Action<string, Exception?>? _reportError;
    private AssetId _openAssetId;
    private Guid _rootEntityId;
    private bool _synchronizing;
    private bool _disposed;

    public BlueprintEditingService(
        AssetDatabase assets,
        AssetEditingService assetEditing,
        GameAssemblyLoadService assemblies,
        Action<string, Exception?>? reportError = null)
    {
        _assets = assets;
        _assetEditing = assetEditing;
        _assemblies = assemblies;
        _reportError = reportError;
        Selection = new SelectionService();
        _assetEditing.Changed += OnAssetEditingChanged;
        _assetEditing.PreviewChanged += OnAssetPreviewChanged;
        _assemblies.Reloading += OnAssemblyReloading;
        _assemblies.Reloaded += OnAssemblyReloaded;
    }

    public SceneDocument? Current { get; private set; }
    public SelectionService Selection { get; }
    public string? Error { get; private set; }

    public void Open(AssetRecord asset)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (asset.Kind != AssetKind.Blueprint)
            throw new ArgumentException("The asset is not an Entity Blueprint.", nameof(asset));
        _openAssetId = asset.Id;
        _assetEditing.Select(asset);
        if (Current is null || _assetEditing.Current?.Asset.Id != asset.Id)
            RebuildFromAssetDocument();
    }

    /// <summary>
    /// The authored Blueprint root. The editor host also creates parentless,
    /// editor-only entities (for example its preview camera), so root identity
    /// must not be inferred by counting every parentless runtime entity.
    /// </summary>
    public Entity? Root => FindAuthoredRoot(Current, _rootEntityId);

    internal static Entity? FindAuthoredRoot(
        SceneDocument? document,
        Guid rootEntityId) =>
        rootEntityId == Guid.Empty
            ? null
            : document?.Scene?.FindEntity(rootEntityId);

    private void RebuildFromAssetDocument()
    {
        if (_synchronizing || _disposed)
            return;
        var assetDocument = _assetEditing.Current;
        if (assetDocument?.Instance is not EntityBlueprint blueprint ||
            (_openAssetId != default && assetDocument.Asset.Id != _openAssetId))
        {
            CloseDocument();
            return;
        }

        try
        {
            var selectedIds = Selection.EntityIds.ToArray();
            var clone = DreambitJson.Deserialize<EntityBlueprint>(assetDocument.CaptureJson())
                        ?? throw new InvalidDataException("The Blueprint file is empty.");
            clone.AssetId = assetDocument.Asset.Id;
            clone.AssetName = assetDocument.Asset.LogicalAssetName;
            var rootEntityId = clone.Guid;
            var replacement = new SceneDocument(
                new SceneBlueprint { Name = clone.Name, Entities = [clone] },
                null,
                Selection,
                _reportError,
                ResolveBlueprintInstance);
            replacement.Changed += OnSceneDocumentChanged;
            var previous = Current;
            Current = replacement;
            _rootEntityId = rootEntityId;
            previous?.Dispose();
            Selection.Restore(selectedIds);
            Selection.RemoveMissing(Current.Scene);
            Error = null;
        }
        catch (Exception exception)
        {
            Error = exception.Message;
            _reportError?.Invoke("Could not open the Blueprint hierarchy.", exception);
            CloseDocument();
        }
    }

    private void OnSceneDocumentChanged(SceneDocument sceneDocument)
    {
        var assetDocument = _assetEditing.Current;
        if (_synchronizing || assetDocument?.Instance is not EntityBlueprint ||
            assetDocument.Asset.Id != _openAssetId)
            return;
        try
        {
            _synchronizing = true;
            assetDocument.ReplaceBlueprint("Edit Blueprint Hierarchy", sceneDocument.CaptureSingleRoot());
            Error = null;
        }
        catch (Exception exception)
        {
            Error = exception.Message;
            _reportError?.Invoke("Could not update the Blueprint asset from its hierarchy.", exception);
        }
        finally
        {
            _synchronizing = false;
        }
    }

    private EntityBlueprint ResolveBlueprintInstance(BlueprintInstanceReference instance)
    {
        var asset = _assets.GetSnapshot().Assets.FirstOrDefault(candidate =>
            candidate.Kind == AssetKind.Blueprint &&
            ((instance.AssetId != Guid.Empty && candidate.Id.Value == instance.AssetId) ||
             (!string.IsNullOrWhiteSpace(instance.AssetName) &&
              string.Equals(candidate.LogicalAssetName, instance.AssetName, StringComparison.OrdinalIgnoreCase))))
                    ?? throw new FileNotFoundException($"Blueprint asset '{instance.AssetName}' is not present in this project.");
        var path = Path.Combine(
            _assets.ContentRoot,
            asset.RelativePath.Replace('/', Path.DirectorySeparatorChar));
        var blueprint = DreambitJson.Deserialize<EntityBlueprint>(File.ReadAllText(path))
                        ?? throw new InvalidDataException($"Blueprint '{asset.RelativePath}' is empty.");
        blueprint.AssetId = asset.Id;
        blueprint.AssetName = asset.LogicalAssetName;
        return blueprint;
    }

    private void OnAssetEditingChanged()
    {
        if (_assetEditing.Current?.Asset.Id == _openAssetId)
            RebuildFromAssetDocument();
        else
            CloseDocument();
    }

    private void OnAssetPreviewChanged(DreambitAssetDocument document)
    {
        if (!_synchronizing && document.Asset.Id == _openAssetId)
            RebuildFromAssetDocument();
    }

    private void OnAssemblyReloading(LoadedGameAssembly? _)
    {
        if (Current is null)
            return;
        Current.Changed -= OnSceneDocumentChanged;
        Current.Dispose();
        Current = null;
    }

    private void OnAssemblyReloaded(LoadedGameAssembly _) => RebuildFromAssetDocument();

    private void CloseDocument()
    {
        if (Current is not null)
            Current.Changed -= OnSceneDocumentChanged;
        Current?.Dispose();
        Current = null;
        _rootEntityId = Guid.Empty;
        Selection.Clear();
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _assetEditing.Changed -= OnAssetEditingChanged;
        _assetEditing.PreviewChanged -= OnAssetPreviewChanged;
        _assemblies.Reloading -= OnAssemblyReloading;
        _assemblies.Reloaded -= OnAssemblyReloaded;
        CloseDocument();
        _disposed = true;
    }
}
