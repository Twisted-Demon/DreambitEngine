using System;
using System.Collections.Generic;
using System.Linq;
using Dreambit.ECS;

namespace Dreambit.LDtk;

public enum LDtkLevelLoadMode
{
    All,
    Selected,
}

/// <summary>
/// Base scene for one LDtk world. It loads every level by default, or a selected
/// initial set when configured for streaming. Raw source models are cached by
/// LDtkManager while materialized level instances remain owned by this scene.
/// </summary>
public class LDtkScene : Scene
{
    private readonly string _projectAssetName;
    private readonly string _worldIdentifier;
    private readonly Guid? _worldIid;
    private readonly LDtkLevelLoadMode _loadMode;
    private readonly string[] _initialLevelIdentifiers;
    private readonly Dictionary<Guid, LDtkLevelInstance> _loadedLevels = [];
    private readonly LDtkLevelImporter _importer = new();

    protected LDtkScene(
        string projectAssetName,
        string worldIdentifier = null,
        LDtkLevelLoadMode loadMode = LDtkLevelLoadMode.All,
        params string[] initialLevelIdentifiers)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectAssetName);
        _projectAssetName = projectAssetName;
        _worldIdentifier = worldIdentifier;
        _loadMode = loadMode;
        _initialLevelIdentifiers = initialLevelIdentifiers ?? [];
    }

    protected LDtkScene(
        string projectAssetName,
        Guid worldIid,
        LDtkLevelLoadMode loadMode = LDtkLevelLoadMode.All,
        params string[] initialLevelIdentifiers)
        : this(projectAssetName, null, loadMode, initialLevelIdentifiers)
    {
        _worldIid = worldIid;
    }

    public LDtkFile Project { get; private set; }
    public LDtkLoadedWorld World { get; private set; }
    public IReadOnlyDictionary<Guid, LDtkLevelInstance> LoadedLevels => _loadedLevels;

    protected sealed override void OnInitialize()
    {
        var manager = LDtkManager.Instance;
        manager.Initialize(_projectAssetName);
        Project = manager.LDtkProject;
        World = SelectWorld(manager);

        OnBeforeLDtkLevelsLoaded();
        switch (_loadMode)
        {
            case LDtkLevelLoadMode.All:
                LoadAllLevels();
                break;
            case LDtkLevelLoadMode.Selected:
                foreach (var identifier in GetInitiallyLoadedLevelIdentifiers()
                             .Where(identifier => !string.IsNullOrWhiteSpace(identifier))
                             .Distinct(StringComparer.Ordinal))
                    LoadLevel(identifier);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(_loadMode));
        }

        OnLDtkSceneReady();
    }

    protected sealed override void OnEnd()
    {
        OnLDtkSceneEnding();
        foreach (var iid in _loadedLevels.Keys.ToArray())
            UnloadLevel(iid);
    }

    public LDtkLevelInstance LoadLevel(string identifier)
    {
        CheckWorld();
        var level = LDtkManager.Instance.LoadLevel(World, identifier);
        return ImportLevel(level);
    }

    public LDtkLevelInstance LoadLevel(Guid iid)
    {
        CheckWorld();
        var level = LDtkManager.Instance.LoadLevel(World, iid);
        return ImportLevel(level);
    }

    public IReadOnlyList<LDtkLevelInstance> LoadAllLevels()
    {
        CheckWorld();
        var loaded = new List<LDtkLevelInstance>(World.Levels.Count);
        foreach (var level in World.Levels)
            loaded.Add(LoadLevel(level.Iid));
        return loaded;
    }

    public bool UnloadLevel(string identifier)
    {
        var instance = _loadedLevels.Values.FirstOrDefault(level =>
            string.Equals(level.Identifier, identifier, StringComparison.Ordinal));
        return instance is not null && UnloadLevel(instance.Iid);
    }

    public bool UnloadLevel(Guid iid)
    {
        if (!_loadedLevels.Remove(iid, out var instance))
            return false;

        OnLDtkLevelUnloading(instance);
        instance.Unload();
        OnLDtkLevelUnloaded(instance.Level);
        return true;
    }

    public bool IsLevelLoaded(Guid iid) => _loadedLevels.ContainsKey(iid);

    public bool IsLevelLoaded(string identifier)
        => _loadedLevels.Values.Any(level =>
            string.Equals(level.Identifier, identifier, StringComparison.Ordinal));

    protected virtual LDtkImportOptions CreateLDtkImportOptions() => new();

    protected virtual IEnumerable<string> GetInitiallyLoadedLevelIdentifiers()
        => _initialLevelIdentifiers;

    /// <summary>Called after the project/world are selected and before initial levels are imported.</summary>
    protected virtual void OnBeforeLDtkLevelsLoaded()
    {
    }

    protected virtual void OnLDtkEntityInstances(
        LDtkLevelInstance level,
        IReadOnlyList<EntityInstance> entityInstances)
    {
        foreach (var entityInstance in entityInstances)
        {
            var ldtkEntity = level.CreateEntityData(entityInstance);
            var entity = CreateLDtkEntity(level, ldtkEntity);
            if (entity is null)
                continue;

            ResetSpawnedHierarchyTransforms(entity);
            level.ApplyDrawLayer(entity, ldtkEntity.Instance);
            level.TrackEntity(entity);
        }
    }

    protected virtual void OnLDtkLevelLoaded(LDtkLevelInstance level)
    {
    }

    protected virtual void OnLDtkLevelUnloading(LDtkLevelInstance level)
    {
    }

    protected virtual void OnLDtkLevelUnloaded(LDtkLevel level)
    {
    }

    /// <summary>Called after all configured initial levels have been imported.</summary>
    protected virtual void OnLDtkSceneReady()
    {
    }

    protected virtual void OnLDtkSceneEnding()
    {
    }

    private Entity CreateLDtkEntity(LDtkLevelInstance level, LDtkEntity ldtkEntity)
    {
        if (LDtkEntityBuilderRepository.TryGetEntityBuilder(ldtkEntity.Identifier, out var builder))
            return builder.BuildEntity(this, level, ldtkEntity);

        if (ldtkEntity.TryGetField<string>("Blueprint", out var blueprintPath) &&
            !string.IsNullOrWhiteSpace(blueprintPath))
            return CreateBlueprintEntity(level, ldtkEntity, blueprintPath);

        Logger.Info($"Generating Entity: {ldtkEntity.Identifier}");
        var entity = CreateEntity(
            ldtkEntity.Identifier,
            createAt: ldtkEntity.Position.ToVector3(),
            tags: [..ldtkEntity.Tags]);
        entity.Parent = level.RootEntity;
        return entity;
    }

    private Entity CreateBlueprintEntity(
        LDtkLevelInstance level,
        LDtkEntity ldtkEntity,
        string blueprintPath)
    {
        var blueprint = Resources.LoadAsset<EntityBlueprint>(blueprintPath);
        if (blueprint is null)
        {
            Logger.Warn(
                $"Could not load blueprint '{blueprintPath}' for LDtk entity " +
                $"'{ldtkEntity.Identifier}' ({ldtkEntity.Iid}).");
            return null;
        }

        Logger.Info($"Loading Entity {ldtkEntity.Identifier} from Blueprint: {blueprintPath}");
        return CreateChildOfEntity(
            blueprint,
            level.RootEntity,
            createAt: ldtkEntity.Position.ToVector3());
    }

    private LDtkLoadedWorld SelectWorld(LDtkManager manager)
    {
        if (_worldIid.HasValue)
            return manager.LoadWorld(_worldIid.Value);
        return string.IsNullOrWhiteSpace(_worldIdentifier)
            ? manager.LoadWorld()
            : manager.LoadWorld(_worldIdentifier);
    }

    private LDtkLevelInstance ImportLevel(LDtkLevel level)
    {
        if (_loadedLevels.TryGetValue(level.Iid, out var cached))
            return cached;

        var instance = _importer.Import(this, World, level, CreateLDtkImportOptions());
        _loadedLevels.Add(level.Iid, instance);
        try
        {
            OnLDtkEntityInstances(instance, instance.EntityInstances);
            OnLDtkLevelLoaded(instance);
            return instance;
        }
        catch
        {
            _loadedLevels.Remove(level.Iid);
            instance.Unload();
            throw;
        }
    }

    private void CheckWorld()
    {
        if (World is null)
            throw new InvalidOperationException("The LDtk scene has not initialized its world yet.");
    }

    private static void ResetSpawnedHierarchyTransforms(Entity entity)
    {
        entity.Transform.ResetLastWorldPosition();
        foreach (var child in entity.GetChildren())
            child.Transform.ResetLastWorldPosition();
    }
}
