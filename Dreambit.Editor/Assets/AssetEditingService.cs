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

    public void Select(AssetRecord? asset)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        Current?.Dispose();
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
    }

    public void Clear() => Select(null);

    private Type? ResolveAssetType(AssetRecord asset)
    {
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
        document.Dispose();
        Current = null;
        Changed?.Invoke();
    }

    private void OnReloaded(LoadedGameAssembly _)
    {
        if (_selected is not null)
        {
            Select(_selected);
            if (Current is not null && _reloadSnapshot is not null)
                Current.RestoreReloadSnapshot(_reloadSnapshot, _reloadDirty);
        }
        _reloadSnapshot = null;
        _reloadDirty = false;
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _assemblies.Reloading -= OnReloading;
        _assemblies.Reloaded -= OnReloaded;
        Current?.Dispose();
        Current = null;
        _disposed = true;
    }
}
