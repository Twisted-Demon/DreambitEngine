using System.Diagnostics;
using Dreambit.ECS;
using Dreambit.Editor.Undo;
using Dreambit.LDtk;
using Dreambit.Tiled;
using Newtonsoft.Json.Linq;

namespace Dreambit.Editor.Scenes;

internal enum SceneDocumentHistoryOwnership
{
    Document,
    External
}

internal sealed class SceneDocument : IDisposable
{
    private readonly Action<string, Exception?>? _reportError;
    private readonly Func<BlueprintInstanceReference, EntityBlueprint>? _blueprintInstanceResolver;
    private readonly Func<LDtkSceneReference, LDtkFile>? _ldtkProjectResolver;
    private readonly Func<TiledSceneReference, TmxMap>? _tiledMapResolver;
    private readonly SceneDocumentHistoryOwnership _historyOwnership;
    private SceneBlueprint _source;
    private string? _savedSnapshot;
    private readonly HashSet<string> _explicitlyClearedReferences = new(StringComparer.Ordinal);
    private readonly HashSet<string> _explicitlyRemovedComponents = new(StringComparer.OrdinalIgnoreCase);
    private long _lastChangeTimestamp;
    private int _sceneGeneration;
    private string? _activeChangeMergeKey;
    private SceneEditTransaction? _activeTransaction;
    private bool _disposed;

    public SceneDocument(
        SceneBlueprint source,
        string? path,
        SelectionService selection,
        Action<string, Exception?>? reportError = null,
        Func<BlueprintInstanceReference, EntityBlueprint>? blueprintInstanceResolver = null,
        Func<LDtkSceneReference, LDtkFile>? ldtkProjectResolver = null,
        SceneDocumentHistoryOwnership historyOwnership = SceneDocumentHistoryOwnership.Document,
        Func<TiledSceneReference, TmxMap>? tiledMapResolver = null)
    {
        _source = source;
        Path = path;
        Selection = selection;
        Undo = new UndoService();
        _reportError = reportError;
        _blueprintInstanceResolver = blueprintInstanceResolver;
        _ldtkProjectResolver = ldtkProjectResolver;
        _tiledMapResolver = tiledMapResolver;
        _historyOwnership = historyOwnership;
        RebuildLiveScene();
        // Dirty comparisons operate on captured snapshots, so establish the saved
        // baseline in that same canonical representation.
        _savedSnapshot = CaptureJson();
    }

    public EditorScene? Scene { get; private set; }
    public SelectionService Selection { get; }
    public UndoService Undo { get; }
    public string? Path { get; private set; }
    public string Name => string.IsNullOrWhiteSpace(_source.Name) ? "Untitled" : _source.Name;
    public string DisplayName => System.IO.Path.GetFileNameWithoutExtension(
        System.IO.Path.GetFileNameWithoutExtension(Path ?? Name));
    public bool IsDirty { get; private set; }
    public bool OwnsEditHistory => _historyOwnership == SceneDocumentHistoryOwnership.Document;
    internal int SceneGeneration => _sceneGeneration;
    internal string? ActiveChangeMergeKey => _activeChangeMergeKey;
    public bool HasLiveScene => Scene is not null;
    public LDtkSceneReference? LDtkReference => _source.LDtk;
    public TiledSceneReference? TiledReference => _source.Tiled;
    public SceneSettings Settings => _source.Settings ??= new SceneSettings();
    public event Action<SceneDocument>? Changed;

    public static SceneDocument CreateNew(
        string name,
        SelectionService selection,
        Action<string, Exception?>? reportError = null,
        Func<BlueprintInstanceReference, EntityBlueprint>? blueprintInstanceResolver = null,
        Func<LDtkSceneReference, LDtkFile>? ldtkProjectResolver = null,
        LDtkSceneReference? ldtk = null,
        SceneDocumentHistoryOwnership historyOwnership = SceneDocumentHistoryOwnership.Document,
        Func<TiledSceneReference, TmxMap>? tiledMapResolver = null,
        TiledSceneReference? tiled = null)
    {
        var document = new SceneDocument(
            new SceneBlueprint { Name = name, Entities = [], LDtk = ldtk, Tiled = tiled },
            null,
            selection,
            reportError,
            blueprintInstanceResolver,
            ldtkProjectResolver,
            historyOwnership,
            tiledMapResolver);
        if (document.OwnsEditHistory)
        {
            // A new scene has no successful on-disk save to compare against.
            document._savedSnapshot = null;
            document.IsDirty = true;
        }
        return document;
    }

    public static SceneDocument Open(
        string path,
        SelectionService selection,
        Action<string, Exception?>? reportError = null,
        Func<BlueprintInstanceReference, EntityBlueprint>? blueprintInstanceResolver = null,
        Func<LDtkSceneReference, LDtkFile>? ldtkProjectResolver = null,
        SceneDocumentHistoryOwnership historyOwnership = SceneDocumentHistoryOwnership.Document,
        Func<TiledSceneReference, TmxMap>? tiledMapResolver = null)
    {
        var fullPath = System.IO.Path.GetFullPath(path);
        var source = SceneDocumentSerializer.Deserialize(File.ReadAllText(fullPath));
        return new SceneDocument(
            source,
            fullPath,
            selection,
            reportError,
            blueprintInstanceResolver,
            ldtkProjectResolver,
            historyOwnership,
            tiledMapResolver);
    }

    public void Update(bool autoSave, TimeSpan autoSaveDelay)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        Scene?.EditorTick();
        if (!autoSave || !IsDirty || Path is null)
            return;
        if (Stopwatch.GetElapsedTime(_lastChangeTimestamp) < autoSaveDelay)
            return;

        try
        {
            Save();
        }
        catch (Exception exception)
        {
            _reportError?.Invoke("Auto Save failed.", exception);
            _lastChangeTimestamp = Stopwatch.GetTimestamp();
        }
    }

    public void Save(string? path = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var targetPath = !string.IsNullOrWhiteSpace(path)
            ? System.IO.Path.GetFullPath(path)
            : Path;
        if (targetPath is null)
            throw new InvalidOperationException("Choose a path before saving this scene.");
        RollBackActiveTransactionBeforeSourceCapture();
        var snapshot = CaptureJson();

        var directory = System.IO.Path.GetDirectoryName(targetPath)!;
        Directory.CreateDirectory(directory);
        var temporaryPath = targetPath + $".{Guid.NewGuid():N}.tmp";
        try
        {
            File.WriteAllText(temporaryPath, snapshot);
            File.Move(temporaryPath, targetPath, true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }

        Path = targetPath;
        _savedSnapshot = snapshot;
        IsDirty = false;
    }

    public void Apply(
        string name,
        Action<EditorScene> mutation,
        string? mergeKey = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(mutation);
        if (_activeTransaction is not null)
        {
            throw new InvalidOperationException(
                "Finish the active scene edit transaction before applying another mutation.");
        }
        var scene = Scene ?? throw new InvalidOperationException("The live scene is unavailable during reload.");
        var before = CaptureJson();
        var beforeSelection = Selection.EntityIds.ToArray();
        var beforeComponents = CaptureLiveComponentKeys(scene);
        string after;
        try
        {
            mutation(scene);
            scene.FlushStructuralChanges();
            Selection.RemoveMissing(scene);
            MarkRemovedComponents(beforeComponents, scene);
            after = CaptureJson();
        }
        catch (Exception exception)
        {
            RollBackFailedMutation(before, beforeSelection, exception);
            throw;
        }

        var afterSelection = Selection.EntityIds.ToArray();
        if (string.Equals(before, after, StringComparison.Ordinal))
        {
            UpdateDirtyState(after);
            return;
        }

        CommitSnapshotChange(
            name,
            before,
            after,
            beforeSelection,
            afterSelection,
            mergeKey);
    }

    public SceneEditTransaction BeginTransaction(string name)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (Scene is null)
            throw new InvalidOperationException("The live scene is unavailable during reload.");
        if (_activeTransaction is not null)
            throw new InvalidOperationException("Only one scene edit transaction can be active at a time.");
        var transaction = new SceneEditTransaction(
            this,
            name,
            CaptureJson(),
            Selection.EntityIds.ToArray(),
            CaptureLiveComponentKeys(Scene),
            _sceneGeneration);
        _activeTransaction = transaction;
        return transaction;
    }

    public Entity CreateEmpty(string name = "Entity", Entity? parent = null)
    {
        if (parent?.IsImportedMapGenerated == true)
            throw new InvalidOperationException("Imported map entities cannot own Dreambit-authored children.");
        if (parent is not null && TryGetBlueprintInstanceRoot(parent, out _, out _))
            throw new InvalidOperationException("Unbox the Blueprint instance before adding children to it.");
        Entity? created = null;
        Apply("Create Entity", scene =>
        {
            created = scene.CreateEntity(name);
            if (parent is not null)
                created.SetParent(parent, false);
            scene.FlushStructuralChanges();
            Selection.Set(created);
        });
        return created!;
    }

    public void Rename(Entity entity, string name)
    {
        if (TryGetBlueprintInstanceRoot(entity, out _, out _))
            throw new InvalidOperationException("Unbox the Blueprint instance before renaming linked entities.");
        var trimmed = name.Trim();
        if (trimmed.Length == 0 || string.Equals(entity.Name, trimmed, StringComparison.Ordinal))
            return;
        Apply("Rename Entity", _ =>
        {
            entity.Name = trimmed;
            RecordGeneratedEntityName(entity);
        });
    }

    public void SetEntityEnabled(
        IReadOnlyList<Entity> entities,
        bool enabled,
        string? mergeKey = null)
    {
        ArgumentNullException.ThrowIfNull(entities);
        if (entities.Count == 0)
            return;
        Apply("Set Enabled", _ =>
        {
            foreach (var entity in entities)
            {
                entity.Enabled = enabled;
                RecordGeneratedEntityEnabled(entity);
            }
        }, mergeKey);
    }

    public void SetEntityTags(
        IReadOnlyList<Entity> entities,
        IReadOnlyCollection<string> tags,
        string? mergeKey = null)
    {
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(tags);
        if (entities.Count == 0)
            return;
        Apply("Change Entity Tags", _ =>
        {
            foreach (var entity in entities)
            {
                entity.Tags.Clear();
                entity.Tags.UnionWith(tags);
                RecordGeneratedEntityTags(entity);
            }
        }, mergeKey);
    }

    public void SetEntityPosition(
        IReadOnlyList<Entity> entities,
        Microsoft.Xna.Framework.Vector3 position,
        string? mergeKey = null)
    {
        ArgumentNullException.ThrowIfNull(entities);
        if (entities.Count == 0)
            return;
        Apply("Change Position", _ =>
        {
            foreach (var entity in entities)
            {
                entity.Transform.Position = position;
                RecordGeneratedPosition(entity);
            }
        }, mergeKey);
    }

    public void SetEntityRotation(
        IReadOnlyList<Entity> entities,
        float rotation,
        string? mergeKey = null)
    {
        ArgumentNullException.ThrowIfNull(entities);
        if (entities.Count == 0)
            return;
        Apply("Change Rotation", _ =>
        {
            foreach (var entity in entities)
            {
                entity.Transform.Rotation2D = rotation;
                RecordGeneratedRotation(entity);
            }
        }, mergeKey);
    }

    public void SetEntityScale(
        IReadOnlyList<Entity> entities,
        Microsoft.Xna.Framework.Vector3 scale,
        string? mergeKey = null)
    {
        ArgumentNullException.ThrowIfNull(entities);
        if (entities.Count == 0)
            return;
        Apply("Change Scale", _ =>
        {
            foreach (var entity in entities)
            {
                entity.Transform.Scale = scale;
                RecordGeneratedScale(entity);
            }
        }, mergeKey);
    }

    public Entity Duplicate(Entity entity)
    {
        if (entity.IsImportedMapGenerated)
            throw new InvalidOperationException("Imported map entities are recreated from their source and cannot be duplicated.");
        if (TryGetBlueprintInstanceRoot(entity, out var instanceRoot, out _) &&
            !ReferenceEquals(entity, instanceRoot))
        {
            throw new InvalidOperationException("Duplicate the boxed Blueprint root, or unbox it first.");
        }
        Entity? duplicated = null;
        Apply("Duplicate Entity", scene =>
        {
            var captured = SceneDocumentSerializer.CaptureSubtree(scene, _source, entity);
            var clone = SceneDocumentSerializer.CloneAndRemap(captured);
            _source.Entities.Add(clone);
            try
            {
                scene.LoadIntoSelf(
                    new SceneBlueprint { Name = Name, Entities = [clone] },
                    CreateEditorLoadOptions());
            }
            catch
            {
                _source.Entities.Remove(clone);
                throw;
            }
            scene.FlushStructuralChanges();
            duplicated = scene.FindEntity(clone.Guid);
            if (duplicated is not null && entity.Parent is not null)
                duplicated.SetParent(entity.Parent, false);
            Selection.Set(duplicated);
        });
        return duplicated!;
    }

    public Entity InstantiateBlueprint(
        EntityBlueprint blueprint,
        Microsoft.Xna.Framework.Vector3? worldPosition = null,
        Entity? parent = null)
    {
        ArgumentNullException.ThrowIfNull(blueprint);
        if (parent?.IsImportedMapGenerated == true)
            throw new InvalidOperationException("Imported map entities cannot own Dreambit-authored children.");
        Entity? created = null;
        Apply("Instantiate Blueprint", scene =>
        {
            EntityBlueprint clone;
            if (!blueprint.AssetId.IsEmpty || !string.IsNullOrWhiteSpace(blueprint.AssetName))
            {
                clone = new EntityBlueprint
                {
                    Name = blueprint.Name,
                    Guid = Guid.NewGuid(),
                    Position = blueprint.Position,
                    Rotation = blueprint.Rotation,
                    Scale = blueprint.Scale,
                    BlueprintInstance = new BlueprintInstanceReference
                    {
                        AssetId = blueprint.AssetId.Value,
                        AssetName = blueprint.AssetName ?? string.Empty
                    }
                };
                _source.Entities.Add(clone);
            }
            else
            {
                clone = SceneDocumentSerializer.CloneAndRemap(blueprint);
            }

            try
            {
                scene.LoadIntoSelf(
                    new SceneBlueprint { Name = Name, Entities = [clone] },
                    CreateEditorLoadOptions());
            }
            catch
            {
                _source.Entities.Remove(clone);
                throw;
            }
            scene.FlushStructuralChanges();
            created = scene.FindEntity(clone.Guid)
                      ?? throw new InvalidOperationException("The Blueprint did not create an entity.");
            if (parent is not null)
                created.SetParent(parent, false);
            if (worldPosition.HasValue)
                created.Transform.WorldPosition = worldPosition.Value;
            Selection.Set(created);
        });
        return created!;
    }

    public bool TryGetBlueprintInstanceRoot(Entity entity, out Entity root, out BlueprintInstanceReference instance)
    {
        ArgumentNullException.ThrowIfNull(entity);
        for (var candidate = entity; candidate is not null; candidate = candidate.Parent)
        {
            var source = FindSourceEntity(candidate.Id);
            if (source?.BlueprintInstance is not { } linked)
                continue;
            root = candidate;
            instance = linked;
            return true;
        }

        root = null!;
        instance = null!;
        return false;
    }

    public bool IsBlueprintInstanceRoot(Entity entity) =>
        FindSourceEntity(entity.Id)?.BlueprintInstance is not null;

    public void UnboxBlueprint(Entity entity)
    {
        if (!TryGetBlueprintInstanceRoot(entity, out var root, out var instance))
            return;
        Apply("Unbox Blueprint Instance", _ =>
        {
            var resolver = _blueprintInstanceResolver
                           ?? throw new InvalidOperationException(
                               "The Blueprint instance cannot be unboxed without its source resolver.");
            var resolved = resolver(instance)
                           ?? throw new InvalidOperationException(
                               $"Could not resolve Blueprint instance '{instance.AssetName}'.");
            var authored = SceneDocumentSerializer.CloneAuthoredForUnboxing(resolved, root);

            // The scene instance owns the root placement. Everything beneath it comes from
            // the authored Blueprint, including nested boxed Blueprint references.
            authored.Position = root.Transform.Position;
            authored.Rotation = new Microsoft.Xna.Framework.Vector3(0f, 0f, root.Transform.Rotation2D);
            authored.Scale = root.Transform.Scale;

            if (!ReplaceSourceEntity(_source.Entities, root.Id, authored))
                throw new InvalidOperationException("The Blueprint instance source was not found.");
        });
    }

    public void Delete(IEnumerable<Entity> entities)
    {
        var roots = RemoveDescendantDuplicates(entities).ToArray();
        if (roots.Length == 0)
            return;
        if (roots.Any(entity => entity.IsImportedMapGenerated))
            throw new InvalidOperationException(
                "Imported map entities are recreated from their source and cannot be deleted directly.");
        if (roots.Any(entity =>
                TryGetBlueprintInstanceRoot(entity, out var instanceRoot, out _) &&
                !ReferenceEquals(entity, instanceRoot)))
        {
            throw new InvalidOperationException("Delete the boxed Blueprint root, or unbox it first.");
        }

        Apply(roots.Length == 1 ? "Delete Entity" : "Delete Entities", scene =>
        {
            foreach (var root in roots)
                DestroyHierarchy(scene, root);
            Selection.Clear();
        });
    }

    public void Reparent(Entity entity, Entity? parent, bool preserveWorldTransform = true)
    {
        if (ReferenceEquals(entity.Parent, parent))
            return;
        if (entity.IsImportedMapGenerated || parent?.IsImportedMapGenerated == true)
            throw new InvalidOperationException("Imported map hierarchy structure is owned by its source map.");
        if (TryGetBlueprintInstanceRoot(entity, out var instanceRoot, out _) &&
            !ReferenceEquals(entity, instanceRoot))
        {
            throw new InvalidOperationException("Unbox the Blueprint instance before moving linked children.");
        }
        if (parent is not null && TryGetBlueprintInstanceRoot(parent, out _, out _))
            throw new InvalidOperationException("Unbox the Blueprint instance before parenting entities beneath it.");
        Apply("Reparent Entity", _ => entity.SetParent(parent, preserveWorldTransform));
    }

    public void MarkReferenceCleared(Entity entity, Type componentType, string memberName)
    {
        _explicitlyClearedReferences.Add(
            SceneDocumentSerializer.GetReferenceKey(entity.Id, componentType, memberName));
    }

    public void SetComponentMember(
        string name,
        IReadOnlyList<Component> components,
        string memberName,
        Type valueType,
        object? value,
        Action<Component, object?> setValue,
        string? mergeKey = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(components);
        ArgumentException.ThrowIfNullOrWhiteSpace(memberName);
        ArgumentNullException.ThrowIfNull(valueType);
        ArgumentNullException.ThrowIfNull(setValue);
        if (components.Count == 0)
            return;

        var isReference = typeof(DreambitAsset).IsAssignableFrom(valueType) ||
                          valueType == typeof(Entity) ||
                          typeof(Component).IsAssignableFrom(valueType);
        Apply(name, _ =>
        {
            foreach (var component in components)
            {
                setValue(component, value);
                component.AcknowledgeEditorSerializationFailure(memberName);
                RecordGeneratedComponentMember(component, memberName, value);
                if (value is null && isReference)
                    MarkReferenceCleared(component.Entity, component.GetType(), memberName);
            }
        }, mergeKey);
    }

    public void RecordGeneratedEntityName(Entity entity)
    {
        if (TryGetLDtkOverride(entity, out var entityOverride))
            entityOverride.Name = entity.Name;
        else if (TryGetTiledOverride(entity, out var tiledOverride))
            tiledOverride.Name = entity.Name;
    }

    public void RecordGeneratedEntityEnabled(Entity entity)
    {
        if (TryGetLDtkOverride(entity, out var entityOverride))
            entityOverride.Enabled = entity.LocallyEnabled;
        else if (TryGetTiledOverride(entity, out var tiledOverride))
            tiledOverride.Enabled = entity.LocallyEnabled;
    }

    public void RecordGeneratedEntityTags(Entity entity)
    {
        if (TryGetLDtkOverride(entity, out var entityOverride))
        {
            entityOverride.Tags = new HashSet<string>(
                entity.Tags,
                StringComparer.OrdinalIgnoreCase);
        }
        else if (TryGetTiledOverride(entity, out var tiledOverride))
        {
            tiledOverride.Tags = new HashSet<string>(
                entity.Tags,
                StringComparer.OrdinalIgnoreCase);
        }
    }

    public void RecordGeneratedPosition(Entity entity)
    {
        if (TryGetLDtkOverride(entity, out var entityOverride))
            entityOverride.Position = entity.Transform.Position;
        else if (TryGetTiledOverride(entity, out var tiledOverride))
            tiledOverride.Position = entity.Transform.Position;
    }

    public void RecordGeneratedRotation(Entity entity)
    {
        if (TryGetLDtkOverride(entity, out var entityOverride))
            entityOverride.Rotation2D = entity.Transform.Rotation2D;
        else if (TryGetTiledOverride(entity, out var tiledOverride))
            tiledOverride.Rotation2D = entity.Transform.Rotation2D;
    }

    public void RecordGeneratedScale(Entity entity)
    {
        if (TryGetLDtkOverride(entity, out var entityOverride))
            entityOverride.Scale = entity.Transform.Scale;
        else if (TryGetTiledOverride(entity, out var tiledOverride))
            tiledOverride.Scale = entity.Transform.Scale;
    }

    public void RecordGeneratedComponentMember(Component component, string memberName, object? value)
    {
        var componentType = component.GetType();
        var componentKey = componentType.FullName ?? componentType.AssemblyQualifiedName ?? componentType.Name;
        Dictionary<string, Dictionary<string, JToken>>? components = null;
        if (TryGetLDtkOverride(component.Entity, out var entityOverride))
            components = entityOverride.Components;
        else if (TryGetTiledOverride(component.Entity, out var tiledOverride))
            components = tiledOverride.Components;
        if (components is null)
            return;
        if (!components.TryGetValue(componentKey, out var properties))
        {
            properties = new Dictionary<string, JToken>(StringComparer.OrdinalIgnoreCase);
            components[componentKey] = properties;
        }
        properties[memberName] = DreambitJson.ToToken(value);
    }

    public void RecordLDtkEntityName(Entity entity) => RecordGeneratedEntityName(entity);
    public void RecordLDtkEntityEnabled(Entity entity) => RecordGeneratedEntityEnabled(entity);
    public void RecordLDtkEntityTags(Entity entity) => RecordGeneratedEntityTags(entity);
    public void RecordLDtkPosition(Entity entity) => RecordGeneratedPosition(entity);
    public void RecordLDtkRotation(Entity entity) => RecordGeneratedRotation(entity);
    public void RecordLDtkScale(Entity entity) => RecordGeneratedScale(entity);
    public void RecordLDtkComponentMember(Component component, string memberName, object? value)
        => RecordGeneratedComponentMember(component, memberName, value);

    public void UpdateLDtkImportOptions(
        string name,
        Action<LDtkImportOptions> mutation,
        string? mergeKey = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(mutation);
        RollBackActiveTransactionBeforeSourceCapture();
        var reference = _source.LDtk
                        ?? throw new InvalidOperationException("This scene is not linked to an LDtk project.");
        var before = CaptureJson();
        var beforeSelection = Selection.EntityIds.ToArray();
        string after;
        try
        {
            var updated = (reference.ImportOptions ?? new LDtkImportOptions()).Clone();
            mutation(updated);
            updated.Validate();
            reference.ImportOptions = updated;
            RebuildPreservingSelection();
            after = CaptureJson();
        }
        catch (Exception exception)
        {
            RollBackFailedMutation(before, beforeSelection, exception);
            throw;
        }
        if (string.Equals(before, after, StringComparison.Ordinal))
        {
            UpdateDirtyState(after);
            return;
        }

        CommitSnapshotChange(
            name,
            before,
            after,
            beforeSelection,
            Selection.EntityIds.ToArray(),
            mergeKey);
    }

    public void UpdateTiledImportOptions(
        string name,
        Action<TiledImportOptions> mutation,
        string? mergeKey = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(mutation);
        RollBackActiveTransactionBeforeSourceCapture();
        var reference = _source.Tiled
                        ?? throw new InvalidOperationException("This scene is not linked to a Tiled map.");
        var before = CaptureJson();
        var beforeSelection = Selection.EntityIds.ToArray();
        string after;
        try
        {
            var updated = (reference.ImportOptions ?? new TiledImportOptions()).Clone();
            mutation(updated);
            updated.Validate();
            reference.ImportOptions = updated;
            RebuildPreservingSelection();
            after = CaptureJson();
        }
        catch (Exception exception)
        {
            RollBackFailedMutation(before, beforeSelection, exception);
            throw;
        }
        if (string.Equals(before, after, StringComparison.Ordinal))
        {
            UpdateDirtyState(after);
            return;
        }

        CommitSnapshotChange(
            name,
            before,
            after,
            beforeSelection,
            Selection.EntityIds.ToArray(),
            mergeKey);
    }

    public void UpdateSceneSettings(
        string name,
        Action<SceneSettings> mutation,
        string? mergeKey = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(mutation);
        RollBackActiveTransactionBeforeSourceCapture();

        var before = CaptureJson();
        var beforeSelection = Selection.EntityIds.ToArray();
        string after;
        try
        {
            var updated = Settings.Clone();
            mutation(updated);
            _source.Settings = updated;
            Scene?.ApplySettings(updated);
            after = CaptureJson();
        }
        catch (Exception exception)
        {
            RollBackFailedMutation(before, beforeSelection, exception);
            throw;
        }

        if (string.Equals(before, after, StringComparison.Ordinal))
        {
            UpdateDirtyState(after);
            return;
        }

        CommitSnapshotChange(
            name,
            before,
            after,
            beforeSelection,
            Selection.EntityIds.ToArray(),
            mergeKey);
    }

    /// <summary>Reloads the linked LDtk source while preserving Dreambit-authored scene entities.</summary>
    public void ReimportLDtk()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_source.LDtk is null || Scene is null)
            return;
        RebuildPreservingSelection();
    }

    /// <summary>Reloads the linked TMX source while preserving Dreambit-authored scene entities.</summary>
    public void ReimportTiled()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_source.Tiled is null || Scene is null)
            return;
        RebuildPreservingSelection();
    }

    public void BeforeAssemblyReload()
    {
        if (Scene is null)
        {
            _activeTransaction?.Abandon();
            return;
        }
        Exception? captureFailure = null;
        try
        {
            // Viewport input runs after assembly polling. The document must therefore
            // settle its own gesture before capture; waiting for the viewport would
            // serialize an uncommitted drag without dirty state or undo history.
            RollBackActiveTransactionBeforeSourceCapture();
            CaptureSource();
        }
        catch (Exception exception)
        {
            captureFailure = exception;
        }
        finally
        {
            var scene = Scene;
            Scene = null;
            _sceneGeneration++;
            var cleanupFailure = EditorDisposal.TryDispose(scene);
            if (cleanupFailure is not null)
            {
                _reportError?.Invoke(
                    "Could not fully dispose the previous live scene before game assembly reload. " +
                    "Its editor reference was released and reload will continue.\n" + cleanupFailure,
                    null);
            }
        }

        if (captureFailure is not null)
        {
            _reportError?.Invoke(
                "Could not capture the latest live scene before game assembly reload. " +
                "The last valid authored snapshot will be rebuilt.",
                captureFailure);
        }
    }

    public void AfterAssemblyReload()
    {
        if (_disposed || Scene is not null)
            return;
        RebuildLiveScene();
    }

    public void ReloadContent()
    {
        if (_disposed)
            return;
        BeforeAssemblyReload();
        Resources.RefreshContent();
        AfterAssemblyReload();
    }

    public void RefreshBlueprintInstances()
    {
        if (_disposed || Scene is null)
            return;
        RollBackActiveTransactionBeforeSourceCapture();
        var selection = Selection.EntityIds.ToArray();
        CaptureSource();
        var replacement = BuildLiveScene(_source);
        ReplaceLiveScene(replacement);
        Selection.Restore(selection);
        Selection.RemoveMissing(Scene);
    }

    private void RebuildPreservingSelection()
    {
        RollBackActiveTransactionBeforeSourceCapture();
        var selected = Selection.Resolve(Scene)
            .Select(entity => new SelectionMarker(
                entity.Id,
                entity.LDtkSourceKey,
                entity.TiledSourceKey))
            .ToArray();
        CaptureSource();
        var previous = Scene!;
        var replacement = BuildLiveScene(_source);
        Scene = replacement;
        _sceneGeneration++;
        ReportSceneCleanupFailure(previous, "Could not fully dispose the replaced imported-map editor scene.");
        var restored = selected.Select(marker =>
        {
            if (string.IsNullOrWhiteSpace(marker.LDtkSourceKey) &&
                string.IsNullOrWhiteSpace(marker.TiledSourceKey))
                return marker.EntityId;
            return Scene!.GetAllEntities().FirstOrDefault(entity =>
                (!string.IsNullOrWhiteSpace(marker.LDtkSourceKey) &&
                 entity.LDtkSourceKey == marker.LDtkSourceKey) ||
                (!string.IsNullOrWhiteSpace(marker.TiledSourceKey) &&
                 entity.TiledSourceKey == marker.TiledSourceKey))?.Id ?? Guid.Empty;
        }).Where(id => id != Guid.Empty);
        Selection.Restore(restored);
        Selection.RemoveMissing(Scene);
    }

    private string CaptureJson()
    {
        if (Scene is not null)
            CaptureSource();
        return SceneDocumentSerializer.Serialize(_source);
    }

    /// <summary>
    /// Captures the only authored root in this document. Blueprint editing uses a
    /// SceneDocument so hierarchy, inspector, undo, and reference handling stay
    /// identical to scene editing.
    /// </summary>
    public EntityBlueprint CaptureSingleRoot()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (Scene is not null)
            CaptureSource();
        if (_source.Entities.Count != 1)
            throw new InvalidOperationException(
                $"A Blueprint document must contain exactly one root entity, but contains {_source.Entities.Count}.");
        return DreambitJson.Deserialize<EntityBlueprint>(DreambitJson.Serialize(_source.Entities[0]))
               ?? throw new InvalidDataException("Could not capture the Blueprint root.");
    }

    private void CaptureSource()
    {
        _source = SceneDocumentSerializer.Capture(
            Scene!,
            _source,
            Name,
            _explicitlyClearedReferences,
            _explicitlyRemovedComponents);
        _explicitlyClearedReferences.Clear();
        _explicitlyRemovedComponents.Clear();
    }

    private void CommitSnapshotChange(
        string name,
        string before,
        string after,
        IReadOnlyList<Guid> beforeSelection,
        IReadOnlyList<Guid> afterSelection,
        string? mergeKey = null)
    {
        UpdateDirtyState(after);
        _lastChangeTimestamp = Stopwatch.GetTimestamp();
        if (OwnsEditHistory)
        {
            Undo.Record(new SceneSnapshotCommand(
                name,
                this,
                before,
                after,
                beforeSelection,
                afterSelection,
                mergeKey));
        }
        NotifyChanged(mergeKey);
    }

    private void NotifyChanged(string? mergeKey)
    {
        _activeChangeMergeKey = mergeKey;
        try
        {
            Changed?.Invoke(this);
        }
        finally
        {
            _activeChangeMergeKey = null;
        }
    }

    private void UpdateDirtyState(string snapshot)
    {
        IsDirty = OwnsEditHistory &&
                  (_savedSnapshot is null ||
                   !string.Equals(_savedSnapshot, snapshot, StringComparison.Ordinal));
    }

    private void RollBackFailedMutation(
        string before,
        IReadOnlyList<Guid> beforeSelection,
        Exception mutationException)
    {
        try
        {
            Restore(before, beforeSelection, notifyChanged: false);
        }
        catch (Exception rollbackException)
        {
            throw new AggregateException(
                "The scene mutation failed and its previous snapshot could not be restored.",
                mutationException,
                rollbackException);
        }
    }

    private void Restore(
        string json,
        IReadOnlyList<Guid> selection,
        bool notifyChanged = true)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        // Undo/redo and failed-mutation restoration replace the live scene. Any
        // gesture still pointing at the outgoing generation can only be abandoned.
        _activeTransaction?.Abandon();
        var restoredSource = SceneDocumentSerializer.Deserialize(json);
        var replacement = BuildLiveScene(restoredSource);
        var previous = Scene;
        _source = restoredSource;
        Scene = replacement;
        _sceneGeneration++;
        _explicitlyClearedReferences.Clear();
        _explicitlyRemovedComponents.Clear();
        ReportSceneCleanupFailure(previous, "Could not fully dispose the replaced editor scene.");
        Selection.Restore(selection);
        Selection.RemoveMissing(Scene);
        UpdateDirtyState(json);
        _lastChangeTimestamp = Stopwatch.GetTimestamp();
        if (notifyChanged)
            NotifyChanged(null);
    }

    private void ReplaceLiveScene(EditorScene replacement)
    {
        _activeTransaction?.Abandon();
        var previous = Scene;
        Scene = replacement;
        _sceneGeneration++;
        ReportSceneCleanupFailure(previous, "Could not fully dispose the replaced editor scene.");
    }

    private void ReportSceneCleanupFailure(EditorScene? scene, string message)
    {
        var cleanupFailure = EditorDisposal.TryDispose(scene);
        if (cleanupFailure is not null)
            _reportError?.Invoke(message + "\n" + cleanupFailure, null);
    }

    private void RollBackActiveTransactionBeforeSourceCapture() =>
        _activeTransaction?.RollBackForDocumentLifecycle();

    private void UnregisterTransaction(SceneEditTransaction transaction)
    {
        if (ReferenceEquals(_activeTransaction, transaction))
            _activeTransaction = null;
    }

    private static bool ReplaceSourceEntity(
        IList<EntityBlueprint> entities,
        Guid entityId,
        EntityBlueprint replacement)
    {
        for (var index = 0; index < entities.Count; index++)
        {
            if (entities[index].Guid == entityId)
            {
                entities[index] = replacement;
                return true;
            }
            if (ReplaceSourceEntity(entities[index].Children, entityId, replacement))
                return true;
        }
        return false;
    }

    private static HashSet<string> CaptureLiveComponentKeys(EditorScene scene)
    {
        scene.FlushStructuralChanges();
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entity in scene.GetAllEntities())
        foreach (var component in entity.GetAllComponents())
            keys.Add(SceneDocumentSerializer.GetComponentKey(entity.Id, component.GetType()));
        return keys;
    }

    private void MarkRemovedComponents(IReadOnlySet<string> before, EditorScene scene)
    {
        var after = CaptureLiveComponentKeys(scene);
        foreach (var componentKey in before)
            if (!after.Contains(componentKey))
                _explicitlyRemovedComponents.Add(componentKey);
    }

    private void RebuildLiveScene()
    {
        Scene = BuildLiveScene(_source);
        _sceneGeneration++;
        Selection.RemoveMissing(Scene);
    }

    private EditorScene BuildLiveScene(SceneBlueprint source)
    {
        var scene = new EditorScene();
        try
        {
            scene.LoadIntoSelf(source, CreateEditorLoadOptions());
            scene.FlushStructuralChanges();
            return scene;
        }
        catch
        {
            scene.Dispose();
            throw;
        }
    }

    private EntityBlueprint? FindSourceEntity(Guid entityId) =>
        _source.Entities
            .SelectMany(root => root.FlattenedHierarchy())
            .FirstOrDefault(entity => entity.Guid == entityId);

    private bool TryGetLDtkOverride(Entity entity, out LDtkGeneratedEntityOverride entityOverride)
    {
        if (_source.LDtk is not { } reference || string.IsNullOrWhiteSpace(entity.LDtkSourceKey))
        {
            entityOverride = null!;
            return false;
        }

        reference.EntityOverrides ??= new Dictionary<string, LDtkGeneratedEntityOverride>(StringComparer.Ordinal);
        if (!reference.EntityOverrides.TryGetValue(entity.LDtkSourceKey, out entityOverride!))
        {
            entityOverride = new LDtkGeneratedEntityOverride();
            reference.EntityOverrides[entity.LDtkSourceKey] = entityOverride;
        }
        return true;
    }

    private bool TryGetTiledOverride(Entity entity, out TiledGeneratedEntityOverride entityOverride)
    {
        if (_source.Tiled is not { } reference || string.IsNullOrWhiteSpace(entity.TiledSourceKey))
        {
            entityOverride = null!;
            return false;
        }

        reference.EntityOverrides ??= new Dictionary<string, TiledGeneratedEntityOverride>(StringComparer.Ordinal);
        if (!reference.EntityOverrides.TryGetValue(entity.TiledSourceKey, out entityOverride!))
        {
            entityOverride = new TiledGeneratedEntityOverride();
            reference.EntityOverrides[entity.TiledSourceKey] = entityOverride;
        }
        return true;
    }

    private SceneBlueprintLoadOptions CreateEditorLoadOptions() => new()
    {
        AllowMissingComponentTypes = true,
        PreserveEntityIds = true,
        TolerateComponentLoadErrors = true,
        BlueprintInstanceResolver = _blueprintInstanceResolver,
        LDtkProjectResolver = _ldtkProjectResolver,
        TiledMapResolver = _tiledMapResolver,
        MarkImportedLDtkEntitiesEditorOnly = true,
        MarkImportedTiledEntitiesEditorOnly = true,
        MaterializeLDtkEntities = false
    };

    private static IEnumerable<Entity> RemoveDescendantDuplicates(IEnumerable<Entity> entities)
    {
        var selected = entities.ToHashSet();
        foreach (var entity in selected)
        {
            var ancestor = entity.Parent;
            var hasSelectedAncestor = false;
            while (ancestor is not null)
            {
                if (selected.Contains(ancestor))
                {
                    hasSelectedAncestor = true;
                    break;
                }
                ancestor = ancestor.Parent;
            }
            if (!hasSelectedAncestor)
                yield return entity;
        }
    }

    private static void DestroyHierarchy(Scene scene, Entity entity)
    {
        foreach (var child in entity.Children.ToArray())
            DestroyHierarchy(scene, child);
        entity.SetParent(null, false);
        scene.DestroyEntity(entity);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _activeTransaction?.Abandon();
        _disposed = true;
        _sceneGeneration++;
        var scene = Scene;
        Scene = null;
        Undo.Clear();
        Changed = null;
        scene?.Dispose();
    }

    private sealed class SceneSnapshotCommand(
        string name,
        SceneDocument document,
        string before,
        string after,
        IReadOnlyList<Guid> beforeSelection,
        IReadOnlyList<Guid> afterSelection,
        string? mergeKey) : IUndoableEditorCommand
    {
        public string Name { get; } = name;
        public string? MergeKey { get; } = mergeKey;
        private SceneDocument Document { get; } = document;
        private string Before { get; } = before;
        private string After { get; set; } = after;
        private IReadOnlyList<Guid> BeforeSelection { get; } = beforeSelection;
        private IReadOnlyList<Guid> AfterSelection { get; set; } = afterSelection;
        public bool IsNoOp => string.Equals(Before, After, StringComparison.Ordinal);

        public bool TryMerge(IUndoableEditorCommand subsequent)
        {
            if (subsequent is not SceneSnapshotCommand next ||
                !ReferenceEquals(Document, next.Document) ||
                !string.Equals(MergeKey, next.MergeKey, StringComparison.Ordinal))
            {
                return false;
            }

            After = next.After;
            AfterSelection = next.AfterSelection;
            return true;
        }

        public void Undo() => Document.Restore(Before, BeforeSelection);
        public void Redo() => Document.Restore(After, AfterSelection);
    }

    private readonly record struct SelectionMarker(
        Guid EntityId,
        string? LDtkSourceKey,
        string? TiledSourceKey);

    internal sealed class SceneEditTransaction : IDisposable
    {
        private readonly SceneDocument _document;
        private readonly string _name;
        private readonly string _before;
        private readonly IReadOnlyList<Guid> _beforeSelection;
        private readonly IReadOnlySet<string> _beforeComponents;
        private readonly int _sceneGeneration;
        private bool _finished;

        internal SceneEditTransaction(
            SceneDocument document,
            string name,
            string before,
            IReadOnlyList<Guid> beforeSelection,
            IReadOnlySet<string> beforeComponents,
            int sceneGeneration)
        {
            _document = document;
            _name = name;
            _before = before;
            _beforeSelection = beforeSelection;
            _beforeComponents = beforeComponents;
            _sceneGeneration = sceneGeneration;
        }

        public void Update(Action<EditorScene> mutation)
        {
            var scene = GetActiveScene();
            try
            {
                mutation(scene);
                scene.FlushStructuralChanges();
            }
            catch (Exception exception)
            {
                Finish();
                _document.RollBackFailedMutation(_before, _beforeSelection, exception);
                throw;
            }
        }

        public void Commit()
        {
            if (_finished)
                return;
            try
            {
                GetActiveScene();
            }
            catch
            {
                Finish();
                throw;
            }
            string after;
            try
            {
                _document.MarkRemovedComponents(_beforeComponents, GetActiveScene());
                after = _document.CaptureJson();
            }
            catch (Exception exception)
            {
                Finish();
                _document.RollBackFailedMutation(_before, _beforeSelection, exception);
                throw;
            }

            Finish();
            if (string.Equals(_before, after, StringComparison.Ordinal))
            {
                _document.UpdateDirtyState(after);
                return;
            }

            _document.CommitSnapshotChange(
                _name,
                _before,
                after,
                _beforeSelection,
                _document.Selection.EntityIds.ToArray());
        }

        public void Cancel()
        {
            if (_finished)
                return;
            try
            {
                GetActiveScene();
            }
            catch
            {
                Finish();
                throw;
            }
            Finish();
            _document.Restore(_before, _beforeSelection);
        }

        /// <summary>
        /// Ends an interaction whose document or live scene has already been replaced.
        /// Unlike <see cref="Cancel"/>, this deliberately does not touch document state.
        /// </summary>
        public void Abandon() => Finish();

        internal void RollBackForDocumentLifecycle()
        {
            if (_finished)
            {
                _document.UnregisterTransaction(this);
                return;
            }

            try
            {
                GetActiveScene();
            }
            catch
            {
                Finish();
                throw;
            }

            Finish();
            _document.Restore(_before, _beforeSelection, notifyChanged: false);
        }

        public void Dispose() => Commit();

        private void Finish()
        {
            if (_finished)
                return;
            _finished = true;
            _document.UnregisterTransaction(this);
        }

        private EditorScene GetActiveScene()
        {
            if (_finished)
                throw new InvalidOperationException("The scene edit transaction has finished.");
            ObjectDisposedException.ThrowIf(_document._disposed, _document);
            if (_document._sceneGeneration != _sceneGeneration || _document.Scene is null)
            {
                throw new InvalidOperationException(
                    "The live scene changed while this edit transaction was active. Abandon the stale transaction.");
            }
            return _document.Scene;
        }
    }
}
