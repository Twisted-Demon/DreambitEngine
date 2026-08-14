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
    private readonly BlueprintSourceService _blueprintSources;
    private readonly Action<string, Exception?>? _reportError;
    private long _observedAssetVersion;
    private bool _disposed;

    public SceneDocumentService(
        DreambitProjectDefinition project,
        GameAssemblyLoadService assemblies,
        AssetDatabase assets,
        BlueprintSourceService blueprintSources,
        Action<string, Exception?>? reportError = null)
    {
        _project = project;
        _assemblies = assemblies;
        _assets = assets;
        _blueprintSources = blueprintSources;
        _reportError = reportError;
        _observedAssetVersion = _assets.GetSnapshot().Version;
        Selection = new SelectionService();
        _blueprintSources.Changed += OnBlueprintSourcesChanged;
        _assemblies.Reloading += OnAssemblyReloading;
        _assemblies.Reloaded += OnAssemblyReloaded;
    }

    public SceneDocument? Current { get; private set; }
    public SelectionService Selection { get; }
    public event Action<SceneDocument?>? CurrentChanged;

    public SceneDocument New(string name = "Untitled")
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var replacement = SceneDocument.CreateNew(
            name,
            Selection,
            _reportError,
            ResolveBlueprintInstance,
            ResolveLDtkProject);
        ReplaceCurrent(replacement);
        return replacement;
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

        var sceneName = string.IsNullOrWhiteSpace(worldName)
            ? System.IO.Path.GetFileNameWithoutExtension(asset.Name)
            : worldName;
        var replacement = SceneDocument.CreateNew(
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
        ReplaceCurrent(replacement);
        return replacement;
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
        var replacement = SceneDocument.Open(
            fullPath,
            Selection,
            _reportError,
            ResolveBlueprintInstance,
            ResolveLDtkProject);
        ReplaceCurrent(replacement);
        return replacement;
    }

    public void Save(string? path = null)
    {
        var document = Current ?? throw new InvalidOperationException("No scene is open.");
        document.Save(path is null ? null : ResolveScenePath(path));
    }

    public string ResolveScenePath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var contentRoot = System.IO.Path.TrimEndingDirectorySeparator(
            System.IO.Path.GetFullPath(_project.ContentRootPath));
        var resolved = System.IO.Path.GetFullPath(
            System.IO.Path.IsPathFullyQualified(path)
                ? path
                : System.IO.Path.Combine(contentRoot, path));
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        var contentPrefix = contentRoot + System.IO.Path.DirectorySeparatorChar;
        if (!string.Equals(resolved, contentRoot, comparison) &&
            !resolved.StartsWith(contentPrefix, comparison))
        {
            throw new InvalidOperationException(
                "Scene paths must remain inside the project's raw Assets folder.");
        }

        return resolved;
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
        => _blueprintSources.SetPreview(asset, blueprint);

    public void ClearBlueprintPreviews() => _blueprintSources.ClearPreviews();

    public void Close()
    {
        var current = Current;
        Current = null;
        DisposeDocument(current);
        Selection.Clear();
        CurrentChanged?.Invoke(null);
    }

    private void ReplaceCurrent(SceneDocument replacement)
    {
        ArgumentNullException.ThrowIfNull(replacement);
        var previous = Current;
        Current = replacement;
        DisposeDocument(previous);
        CurrentChanged?.Invoke(replacement);
    }

    private void DisposeDocument(SceneDocument? document)
    {
        if (document is null)
            return;
        var cleanupFailure = EditorDisposal.TryDispose(document);
        if (cleanupFailure is not null)
        {
            _reportError?.Invoke(
                "Could not fully dispose the previous editor scene.\n" + cleanupFailure,
                null);
        }
    }

    private void OnAssemblyReloading(LoadedGameAssembly? _) => Current?.BeforeAssemblyReload();
    private void OnAssemblyReloaded(LoadedGameAssembly _) => Current?.AfterAssemblyReload();
    private void OnBlueprintSourcesChanged() => Current?.RefreshBlueprintInstances();

    private EntityBlueprint ResolveBlueprintInstance(BlueprintInstanceReference instance) =>
        _blueprintSources.Resolve(instance);

    private LDtkFile ResolveLDtkProject(LDtkSceneReference instance)
    {
        var assets = _assets.GetSnapshot().Assets;
        var asset = instance.AssetId != Guid.Empty
            ? assets.FirstOrDefault(candidate => candidate.Id.Value == instance.AssetId)
            : assets.FirstOrDefault(candidate =>
                !string.IsNullOrWhiteSpace(instance.AssetName) &&
                string.Equals(
                    candidate.LogicalAssetName,
                    instance.AssetName,
                    StringComparison.OrdinalIgnoreCase));
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
        _disposed = true;
        _blueprintSources.Changed -= OnBlueprintSourcesChanged;
        _assemblies.Reloading -= OnAssemblyReloading;
        _assemblies.Reloaded -= OnAssemblyReloaded;
        Close();
    }
}
