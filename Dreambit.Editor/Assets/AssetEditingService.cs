using Dreambit.Editor.Compilation;
using Dreambit.Editor.Inspection;
using Dreambit.Editor.Projects;

namespace Dreambit.Editor.Assets;

internal sealed class AssetEditingService : IDisposable
{
    private readonly DreambitProjectDefinition _project;
    private readonly AssetDatabase _assets;
    private readonly EditorTypeRegistry _types;
    private readonly InspectorMetadataCache _metadata;
    private readonly GameAssemblyLoadService _assemblies;
    private readonly Action<string, Exception?>? _reportError;
    private AssetRecord? _selected;
    private string? _reloadSnapshot;
    private bool _reloadDirty;
    private bool _disposed;

    public AssetEditingService(
        DreambitProjectDefinition project,
        AssetDatabase assets,
        EditorTypeRegistry types,
        InspectorMetadataCache metadata,
        GameAssemblyLoadService assemblies,
        Action<string, Exception?>? reportError = null)
    {
        _project = project;
        _assets = assets;
        _types = types;
        _metadata = metadata;
        _assemblies = assemblies;
        _reportError = reportError;
        _assemblies.Reloading += OnReloading;
        _assemblies.Reloaded += OnReloaded;
    }

    public AssetRecord? Selected => _selected;
    public DreambitAssetDocument? Current { get; private set; }
    public event Action? Changed;
    public event Action<DreambitAssetDocument>? PreviewChanged;

    public void Select(AssetRecord? asset)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (Current is { } open && asset is not null && asset.Id == open.Asset.Id)
        {
            _selected = asset;
            return;
        }
        if (Current is { IsDirty: true } current &&
            (asset is null || asset.Id != current.Asset.Id))
        {
            try
            {
                Save();
            }
            catch (Exception exception)
            {
                _reportError?.Invoke(
                    $"Could not save '{current.Asset.RelativePath}'. The asset remains open.",
                    exception);
                return;
            }
        }
        DetachAndDisposeCurrent();
        Current = null;
        _selected = asset;
        if (asset is not null)
        {
            var type = ResolveAssetType(asset);
            if (type is not null)
            {
                try
                {
                    Current = DreambitAssetDocument.Open(
                        asset,
                        Path.Combine(_project.ContentRootPath, asset.RelativePath.Replace('/', Path.DirectorySeparatorChar)),
                        type,
                        _metadata);
                    Current.Changed += OnDocumentChanged;
                }
                catch (Exception exception)
                {
                    _reportError?.Invoke($"Could not inspect '{asset.RelativePath}'.", exception);
                }
            }
        }
        Changed?.Invoke();
    }

    public bool TryCreate(Type assetType, string relativePath, out string? error)
    {
        try
        {
            if (!typeof(DreambitAsset).IsAssignableFrom(assetType) || assetType.IsAbstract)
                throw new InvalidOperationException($"'{assetType.FullName}' is not a creatable Dreambit asset.");
            var normalized = relativePath.Replace('\\', '/').Trim().TrimStart('/');
            if (normalized.Length == 0 || normalized.Contains("../", StringComparison.Ordinal))
                throw new InvalidOperationException("Choose a path inside the Assets folder.");
            var path = Path.GetFullPath(Path.Combine(_project.ContentRootPath, normalized));
            var contentPrefix = Path.TrimEndingDirectorySeparator(_project.ContentRootPath) + Path.DirectorySeparatorChar;
            if (!path.StartsWith(contentPrefix, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Asset path escapes the Assets folder.");
            if (File.Exists(path))
                throw new IOException($"'{normalized}' already exists.");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var instance = (DreambitAsset?)Activator.CreateInstance(assetType)
                           ?? throw new InvalidOperationException($"Could not create '{assetType.FullName}'.");
            if (instance is EntityBlueprint blueprint && string.IsNullOrWhiteSpace(blueprint.Name))
                blueprint.Name = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(path));
            File.WriteAllText(path, DreambitJson.Serialize(instance));
            instance.Dispose();
            _assets.RefreshNow();
            if (_assets.TryGetAsset(normalized, out var created))
                Select(created);
            error = null;
            return true;
        }
        catch (Exception exception)
        {
            error = exception.Message;
            return false;
        }
    }

    public void Update(bool autoSave, TimeSpan delay)
    {
        if (!autoSave || Current is not { IsDirty: true } document ||
            DateTimeOffset.UtcNow - document.LastChangedUtc < delay)
            return;
        Save();
    }

    public void Save()
    {
        if (Current is null)
            return;
        var path = Path.Combine(
            _project.ContentRootPath,
            Current.Asset.RelativePath.Replace('/', Path.DirectorySeparatorChar));
        Current.Save(path);
        // Refresh immediately so the incremental baker sees this exact save on the next frame.
        // The completed bake then rehydrates the open scene from fresh asset instances.
        _assets.RefreshNow();
    }

    public void Clear() => Select(null);

    public void BeforeContentReload()
    {
        var document = Current;
        if (document is null)
            return;
        _reloadSnapshot = document.IsDirty ? document.CaptureJson() : null;
        _reloadDirty = document.IsDirty;
        document.Changed -= OnDocumentChanged;
        document.Dispose();
        Current = null;
        Changed?.Invoke();
    }

    public void AfterContentReload()
    {
        if (_selected is not null)
        {
            var selected = _assets.GetSnapshot().Assets.FirstOrDefault(asset => asset.Id == _selected.Id)
                           ?? _selected;
            Select(selected);
            if (Current is not null && _reloadSnapshot is not null)
                Current.RestoreReloadSnapshot(_reloadSnapshot, _reloadDirty);
        }
        _reloadSnapshot = null;
        _reloadDirty = false;
    }

    private Type? ResolveAssetType(AssetRecord asset)
    {
        if (asset.Kind is AssetKind.Texture or AssetKind.Font or AssetKind.Effect)
            return null;
        if (asset.Kind == AssetKind.Blueprint)
            return typeof(EntityBlueprint);
        if (asset.Kind == AssetKind.Scene || string.IsNullOrWhiteSpace(asset.TypeName))
            return null;
        return _types.AssetTypes.FirstOrDefault(type =>
            string.Equals(type.FullName, asset.TypeName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(type.Name, asset.TypeName, StringComparison.OrdinalIgnoreCase));
    }

    private void OnReloading(LoadedGameAssembly? assembly)
    {
        var document = Current;
        if (document is null || assembly is null || document.AssetType.Assembly != assembly.Assembly)
            return;
        _reloadSnapshot = document.CaptureJson();
        _reloadDirty = document.IsDirty;
        document.Changed -= OnDocumentChanged;
        document.Dispose();
        Current = null;
        Changed?.Invoke();
    }

    private void OnReloaded(LoadedGameAssembly _)
    {
        AfterContentReload();
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _assemblies.Reloading -= OnReloading;
        _assemblies.Reloaded -= OnReloaded;
        DetachAndDisposeCurrent();
        Current = null;
        _disposed = true;
    }

    private void OnDocumentChanged(DreambitAssetDocument document)
    {
        try
        {
            var runtimeAsset = Resources.LoadDreambitAsset(
                document.Asset.Id,
                document.Asset.LogicalAssetName,
                document.AssetType) as DreambitAsset;
            if (runtimeAsset is not null && !ReferenceEquals(runtimeAsset, document.Instance))
                document.CopyInspectableValuesTo(runtimeAsset);
            PreviewChanged?.Invoke(document);
        }
        catch (Exception exception)
        {
            _reportError?.Invoke($"Could not preview '{document.Asset.RelativePath}' in the open scene.", exception);
        }
    }

    private void DetachAndDisposeCurrent()
    {
        if (Current is null)
            return;
        Current.Changed -= OnDocumentChanged;
        Current.Dispose();
    }
}
