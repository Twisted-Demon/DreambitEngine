using System;
using System.Collections.Generic;
using System.Linq;

namespace Dreambit.LDtk;

/// <summary>
/// Global repository for the active LDtk project and its deserialized worlds
/// and levels. Runtime scene instances are deliberately owned by LDtkScene;
/// this manager caches only immutable source models.
/// </summary>
public sealed class LDtkManager : Singleton<LDtkManager>
{
    private readonly Dictionary<Guid, LDtkLoadedWorld> _loadedWorlds = [];
    private readonly Dictionary<Guid, LDtkLevel> _loadedLevels = [];

    public LDtkFile LDtkProject { get; private set; }
    public string ProjectAssetName { get; private set; } = string.Empty;
    public IReadOnlyDictionary<Guid, LDtkLoadedWorld> LoadedWorlds => _loadedWorlds;
    public IReadOnlyDictionary<Guid, LDtkLevel> LoadedLevels => _loadedLevels;

    public void Initialize(string projectAssetName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectAssetName);

        if (LDtkProject is not null &&
            string.Equals(ProjectAssetName, projectAssetName, StringComparison.OrdinalIgnoreCase))
            return;

        var project = Resources.LoadAsset<LDtkFile>(projectAssetName)
                      ?? throw new LdtkException($"Could not load LDtk project asset '{projectAssetName}'.");
        SetProject(project, projectAssetName);
    }

    /// <summary>
    /// Installs an already-deserialized project. This is useful for tooling,
    /// tests, and callers that use LDtkFile.FromFile instead of Resources.
    /// </summary>
    public void SetProject(LDtkFile project, string projectAssetName = null)
    {
        LDtkProject = project ?? throw new ArgumentNullException(nameof(project));
        ProjectAssetName = projectAssetName ?? project.SourcePath;
        ClearCache();
    }

    public LDtkLoadedWorld LoadWorld()
    {
        CheckProject();
        return CacheWorld(LDtkProject.LoadWorld());
    }

    public LDtkLoadedWorld LoadWorld(Guid iid)
    {
        CheckProject();
        return _loadedWorlds.TryGetValue(iid, out var cached)
            ? cached
            : CacheWorld(LDtkProject.LoadWorld(iid));
    }

    public LDtkLoadedWorld LoadWorld(string identifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        CheckProject();

        var cached = _loadedWorlds.Values.FirstOrDefault(world =>
            string.Equals(world.Identifier, identifier, StringComparison.Ordinal));
        return cached ?? CacheWorld(LDtkProject.LoadWorld(identifier));
    }

    public LDtkLevel LoadLevel(LDtkLoadedWorld world, Guid iid)
    {
        ArgumentNullException.ThrowIfNull(world);
        CheckProject();
        return _loadedLevels.TryGetValue(iid, out var cached)
            ? cached
            : CacheLevel(world.LoadLevel(iid));
    }

    public LDtkLevel LoadLevel(LDtkLoadedWorld world, string identifier)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        CheckProject();

        var stub = world.Levels.FirstOrDefault(level =>
            string.Equals(level.Identifier, identifier, StringComparison.Ordinal));
        if (stub is null)
            throw new LdtkException($"No level named '{identifier}' exists in world '{world.Identifier}'.");

        return _loadedLevels.TryGetValue(stub.Iid, out var cached)
            ? cached
            : CacheLevel(world.LoadLevel(stub.Iid));
    }

    public bool TryGetLoadedLevel(Guid iid, out LDtkLevel level)
        => _loadedLevels.TryGetValue(iid, out level);

    public void ClearCache()
    {
        _loadedWorlds.Clear();
        _loadedLevels.Clear();
    }

    public void Reset()
    {
        ClearCache();
        LDtkProject = null;
        ProjectAssetName = string.Empty;
    }

    private LDtkLoadedWorld CacheWorld(LDtkLoadedWorld world)
    {
        _loadedWorlds[world.Iid] = world;
        return world;
    }

    private LDtkLevel CacheLevel(LDtkLevel level)
    {
        _loadedLevels[level.Iid] = level;
        return level;
    }

    private void CheckProject()
    {
        if (LDtkProject is null)
            throw new InvalidOperationException("Initialize LDtkManager with a project before loading worlds or levels.");
    }
}
