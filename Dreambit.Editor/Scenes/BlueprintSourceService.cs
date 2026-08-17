using Dreambit.Editor.Assets;

namespace Dreambit.Editor.Scenes;

/// <summary>
/// Resolves authored Blueprint source for editor documents. Unsaved asset previews take
/// precedence over disk, and a stored asset ID is always authoritative over the legacy name.
/// </summary>
internal sealed class BlueprintSourceService : IDisposable
{
    private readonly AssetDatabase _assets;
    private readonly AssetEditingService? _assetEditing;
    private readonly Dictionary<AssetId, EntityBlueprint> _previews = [];
    private bool _disposed;

    public BlueprintSourceService(
        AssetDatabase assets,
        AssetEditingService? assetEditing = null)
    {
        _assets = assets;
        _assetEditing = assetEditing;
        if (_assetEditing is not null)
        {
            _assetEditing.PreviewChanged += OnAssetPreviewChanged;
            _assetEditing.Saved += OnAssetSaved;
            _assetEditing.Changed += OnAssetEditingChanged;
        }
    }

    public event Action? Changed;

    public EntityBlueprint Load(AssetRecord asset)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(asset);
        if (asset.Kind != AssetKind.Blueprint)
            throw new ArgumentException("The asset is not an Entity Blueprint.", nameof(asset));

        // Explicit resolution must observe the newest editor state even while routine preview
        // synchronization is being coalesced after a burst of inspector changes.
        _assetEditing?.FlushPendingPreview();

        if (_previews.TryGetValue(asset.Id, out var preview))
            return Clone(preview, asset);

        var path = Path.Combine(
            _assets.ContentRoot,
            asset.RelativePath.Replace('/', Path.DirectorySeparatorChar));
        var blueprint = DreambitJson.Deserialize<EntityBlueprint>(File.ReadAllText(path))
                        ?? throw new InvalidDataException($"Blueprint '{asset.RelativePath}' is empty.");
        blueprint.AssetId = asset.Id;
        blueprint.AssetName = asset.LogicalAssetName;
        return blueprint;
    }

    public EntityBlueprint Resolve(BlueprintInstanceReference instance)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(instance);
        var asset = FindAsset(instance)
                    ?? throw new FileNotFoundException(
                        instance.AssetId != Guid.Empty
                            ? $"Blueprint asset ID '{instance.AssetId:D}' is not present in this project."
                            : $"Blueprint asset '{instance.AssetName}' is not present in this project.");
        return Load(asset);
    }

    public void SetPreview(AssetRecord asset, EntityBlueprint blueprint)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(asset);
        ArgumentNullException.ThrowIfNull(blueprint);
        var replacement = Clone(blueprint, asset);
        if (_previews.Remove(asset.Id, out var previous))
            previous.Dispose();
        _previews[asset.Id] = replacement;
        Changed?.Invoke();
    }

    public void ClearPreview(AssetId assetId)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!_previews.Remove(assetId, out var preview))
            return;
        preview.Dispose();
        Changed?.Invoke();
    }

    public void ClearPreviews()
    {
        if (_previews.Count == 0)
            return;
        foreach (var preview in _previews.Values)
            preview.Dispose();
        _previews.Clear();
        Changed?.Invoke();
    }

    private void OnAssetPreviewChanged(DreambitAssetDocument document)
    {
        if (document.Instance is not EntityBlueprint blueprint)
            return;
        if (document.IsDirty)
            SetPreview(document.Asset, blueprint);
        else
        {
            var hadPreview = _previews.ContainsKey(document.Asset.Id);
            ClearPreview(document.Asset.Id);
            // A save can happen before a coalesced dirty preview was ever published. The disk
            // source still changed, so live scene instances must be refreshed once.
            if (!hadPreview)
                Changed?.Invoke();
        }
    }

    private void OnAssetSaved(DreambitAssetDocument document) =>
        ClearPreview(document.Asset.Id);

    private void OnAssetEditingChanged()
    {
        var retainedId = _assetEditing?.Current is
            {
                IsDirty: true,
                Instance: EntityBlueprint
            } current
                ? current.Asset.Id
                : default;
        foreach (var assetId in _previews.Keys.ToArray())
            if (retainedId.IsEmpty || assetId != retainedId)
                ClearPreview(assetId);
    }

    private AssetRecord? FindAsset(BlueprintInstanceReference instance)
    {
        var assets = _assets.GetSnapshot().Assets;
        if (instance.AssetId != Guid.Empty)
        {
            var id = new AssetId(instance.AssetId);
            return assets.FirstOrDefault(asset =>
                asset.Kind == AssetKind.Blueprint && asset.Id == id);
        }

        if (string.IsNullOrWhiteSpace(instance.AssetName))
            return null;

        return assets.FirstOrDefault(asset =>
            asset.Kind == AssetKind.Blueprint &&
            string.Equals(
                asset.LogicalAssetName,
                instance.AssetName,
                StringComparison.OrdinalIgnoreCase));
    }

    private static EntityBlueprint Clone(EntityBlueprint source, AssetRecord asset)
    {
        var clone = DreambitJson.Deserialize<EntityBlueprint>(DreambitJson.Serialize(source))
                    ?? throw new InvalidOperationException("Could not clone the Blueprint preview.");
        clone.AssetId = asset.Id;
        clone.AssetName = asset.LogicalAssetName;
        return clone;
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        if (_assetEditing is not null)
        {
            _assetEditing.PreviewChanged -= OnAssetPreviewChanged;
            _assetEditing.Saved -= OnAssetSaved;
            _assetEditing.Changed -= OnAssetEditingChanged;
        }
        foreach (var preview in _previews.Values)
            preview.Dispose();
        _previews.Clear();
        Changed = null;
    }
}
