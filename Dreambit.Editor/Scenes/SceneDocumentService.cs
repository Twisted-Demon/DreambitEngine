using Dreambit.Editor.Compilation;
using Dreambit.Editor.Projects;
using Dreambit.Editor.Assets;
using Dreambit.LDtk;

namespace Dreambit.Editor.Scenes;

internal sealed class SceneDocumentService : IDisposable
{
    internal sealed record LDtkWorldChoice(string DisplayName, Guid WorldIid);

    private readonly DreambitProjectDefinition _project;
    private readonly GameAssemblyLoadService _assemblies;
    private readonly AssetDatabase _assets;
    private readonly Action<string, Exception?>? _reportError;
    private readonly Dictionary<AssetId, EntityBlueprint> _blueprintPreviews = [];
    private long _observedAssetVersion;
    private bool _disposed;

    public SceneDocumentService(
        DreambitProjectDefinition project,
        GameAssemblyLoadService assemblies,
        AssetDatabase assets,
        Action<string, Exception?>? reportError = null)
    {
        _project = project;
        _assemblies = assemblies;
        _assets = assets;
        _reportError = reportError;
        _observedAssetVersion = _assets.GetSnapshot().Version;
        Selection = new SelectionService();
        _assemblies.Reloading += OnAssemblyReloading;
        _assemblies.Reloaded += OnAssemblyReloaded;
    }

    public SceneDocument? Current { get; private set; }
    public SelectionService Selection { get; }
    public event Action<SceneDocument?>? CurrentChanged;

    public SceneDocument New(string name = "Untitled")
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        Close();
        Current = SceneDocument.CreateNew(
            name,
            Selection,
            _reportError,
            ResolveBlueprintInstance,
            ResolveLDtkProject);
        CurrentChanged?.Invoke(Current);
        return Current;
    }

    public SceneDocument NewFromLDtk(
        AssetRecord asset,
        Guid worldIid,
        string? worldName = null,
        LDtkImportOptions? importOptions = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(asset);
        if (asset.Kind != AssetKind.Ldtk ||
            !asset.RelativePath.EndsWith(".ldtk", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("The selected asset is not an LDtk project.", nameof(asset));

        Close();
        var sceneName = string.IsNullOrWhiteSpace(worldName)
            ? System.IO.Path.GetFileNameWithoutExtension(asset.Name)
            : worldName;
        Current = SceneDocument.CreateNew(
            sceneName,
            Selection,
            _reportError,
            ResolveBlueprintInstance,
            ResolveLDtkProject,
            new LDtkSceneReference
            {
                AssetId = asset.Id.Value,
                AssetName = asset.LogicalAssetName,
                WorldIid = worldIid,
                ImportOptions = (importOptions ?? new LDtkImportOptions()).Clone()
            });
        CurrentChanged?.Invoke(Current);
        return Current;
    }

    public IReadOnlyList<LDtkWorldChoice> GetLDtkWorldChoices(AssetRecord asset)
    {
        ArgumentNullException.ThrowIfNull(asset);
        var project = LoadLDtkSource(asset);
        if (project.AvailableWorlds.Count == 0)
            return [new LDtkWorldChoice("World", Guid.Empty)];
        return project.AvailableWorlds
            .Select(world => new LDtkWorldChoice(world.Identifier, world.Iid))
            .ToArray();
    }

    public SceneDocument Open(string path)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var fullPath = ResolveScenePath(path);
        Close();
        Current = SceneDocument.Open(
            fullPath,
            Selection,
            _reportError,
            ResolveBlueprintInstance,
            ResolveLDtkProject);
        CurrentChanged?.Invoke(Current);
        return Current;
    }

    public void Save(string? path = null)
    {
        var document = Current ?? throw new InvalidOperationException("No scene is open.");
        document.Save(path is null ? null : ResolveScenePath(path));
    }

    public string ResolveScenePath(string path)
    {
        if (System.IO.Path.IsPathFullyQualified(path))
            return System.IO.Path.GetFullPath(path);
        return System.IO.Path.GetFullPath(System.IO.Path.Combine(_project.ContentRootPath, path));
    }

    public void Update(bool autoSave, TimeSpan autoSaveDelay)
    {
        var assetVersion = _assets.GetSnapshot().Version;
        if (assetVersion != _observedAssetVersion)
        {
            _observedAssetVersion = assetVersion;
            if (Current?.LDtkReference is not null)
            {
                try
                {
                    Current.ReimportLDtk();
                }
                catch (Exception exception)
                {
                    _reportError?.Invoke(
                        "Could not live-reimport the linked LDtk project. The editor will retry after the next asset change or bake.",
                        exception);
                }
            }
        }

        Current?.Update(autoSave, autoSaveDelay);
    }

    public void ReloadContent()
    {
        if (Current is not null)
            Current.ReloadContent();
        else
            Resources.RefreshContent();
    }

    public void PreviewBlueprint(AssetRecord asset, EntityBlueprint blueprint)
    {
        var preview = DreambitJson.Deserialize<EntityBlueprint>(DreambitJson.Serialize(blueprint))
                      ?? throw new InvalidOperationException("Could not clone the Blueprint preview.");
        preview.AssetId = asset.Id;
        preview.AssetName = asset.LogicalAssetName;
        _blueprintPreviews[asset.Id] = preview;
        Current?.RefreshBlueprintInstances();
    }

    public void ClearBlueprintPreviews() => _blueprintPreviews.Clear();

    public void Close()
    {
        Current?.Dispose();
        Current = null;
        Selection.Clear();
        CurrentChanged?.Invoke(null);
    }

    private void OnAssemblyReloading(LoadedGameAssembly? _) => Current?.BeforeAssemblyReload();
    private void OnAssemblyReloaded(LoadedGameAssembly _) => Current?.AfterAssemblyReload();

    private EntityBlueprint ResolveBlueprintInstance(BlueprintInstanceReference instance)
    {
        if (instance.AssetId != Guid.Empty &&
            _blueprintPreviews.TryGetValue(new AssetId(instance.AssetId), out var preview))
        {
            return preview;
        }
        var asset = FindProjectAsset(instance)
                    ?? throw new FileNotFoundException(
                        $"Blueprint asset '{instance.AssetName}' is not present in this project.");
        var path = System.IO.Path.Combine(
            _project.ContentRootPath,
            asset.RelativePath.Replace('/', System.IO.Path.DirectorySeparatorChar));
        var blueprint = DreambitJson.Deserialize<EntityBlueprint>(File.ReadAllText(path))
                        ?? throw new InvalidDataException($"Blueprint '{asset.RelativePath}' is empty.");
        blueprint.AssetId = asset.Id;
        blueprint.AssetName = asset.LogicalAssetName;
        return blueprint;
    }

    private AssetRecord? FindProjectAsset(BlueprintInstanceReference instance)
    {
        return _assets.GetSnapshot().Assets.FirstOrDefault(asset =>
            (instance.AssetId != Guid.Empty && asset.Id.Value == instance.AssetId) ||
            (!string.IsNullOrWhiteSpace(instance.AssetName) &&
             string.Equals(asset.LogicalAssetName, instance.AssetName, StringComparison.OrdinalIgnoreCase)));
    }

    private LDtkFile ResolveLDtkProject(LDtkSceneReference instance)
    {
        var asset = _assets.GetSnapshot().Assets.FirstOrDefault(candidate =>
            (instance.AssetId != Guid.Empty && candidate.Id.Value == instance.AssetId) ||
            (!string.IsNullOrWhiteSpace(instance.AssetName) &&
             string.Equals(
                 candidate.LogicalAssetName,
                 instance.AssetName,
                 StringComparison.OrdinalIgnoreCase)));
        if (asset is null || asset.Kind != AssetKind.Ldtk)
            throw new FileNotFoundException(
                $"LDtk project asset '{instance.AssetName}' is not present in this project.");
        return LoadLDtkSource(asset);
    }

    private LDtkFile LoadLDtkSource(AssetRecord asset)
    {
        var path = System.IO.Path.Combine(
            _project.ContentRootPath,
            asset.RelativePath.Replace('/', System.IO.Path.DirectorySeparatorChar));
        return LDtkFile.FromContentFile(path, asset.LogicalAssetName, _project.ContentRootPath);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _assemblies.Reloading -= OnAssemblyReloading;
        _assemblies.Reloaded -= OnAssemblyReloaded;
        Close();
        _disposed = true;
    }
}
