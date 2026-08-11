using System.Diagnostics;
using Dreambit.ECS;
using Dreambit.Editor.Undo;
using Dreambit.LDtk;
using Newtonsoft.Json.Linq;

namespace Dreambit.Editor.Scenes;

internal sealed class SceneDocument : IDisposable
{
    private readonly Action<string, Exception?>? _reportError;
    private readonly Func<BlueprintInstanceReference, EntityBlueprint>? _blueprintInstanceResolver;
    private readonly Func<LDtkSceneReference, LDtkFile>? _ldtkProjectResolver;
    private SceneBlueprint _source;
    private readonly HashSet<string> _explicitlyClearedReferences = new(StringComparer.Ordinal);
    private long _lastChangeTimestamp;
    private bool _disposed;

    public SceneDocument(
        SceneBlueprint source,
        string? path,
        SelectionService selection,
        Action<string, Exception?>? reportError = null,
        Func<BlueprintInstanceReference, EntityBlueprint>? blueprintInstanceResolver = null,
        Func<LDtkSceneReference, LDtkFile>? ldtkProjectResolver = null)
    {
        _source = source;
        Path = path;
        Selection = selection;
        Undo = new UndoService();
        _reportError = reportError;
        _blueprintInstanceResolver = blueprintInstanceResolver;
        _ldtkProjectResolver = ldtkProjectResolver;
        RebuildLiveScene();
    }

    public EditorScene? Scene { get; private set; }
    public SelectionService Selection { get; }
    public UndoService Undo { get; }
    public string? Path { get; private set; }
    public string Name => string.IsNullOrWhiteSpace(_source.Name) ? "Untitled" : _source.Name;
    public string DisplayName => System.IO.Path.GetFileNameWithoutExtension(
        System.IO.Path.GetFileNameWithoutExtension(Path ?? Name));
    public bool IsDirty { get; private set; }
    public bool HasLiveScene => Scene is not null;
    public LDtkSceneReference? LDtkReference => _source.LDtk;

    public static SceneDocument CreateNew(
        string name,
        SelectionService selection,
        Action<string, Exception?>? reportError = null,
        Func<BlueprintInstanceReference, EntityBlueprint>? blueprintInstanceResolver = null,
        Func<LDtkSceneReference, LDtkFile>? ldtkProjectResolver = null,
        LDtkSceneReference? ldtk = null) =>
        new(
            new SceneBlueprint { Name = name, Entities = [], LDtk = ldtk },
            null,
            selection,
            reportError,
            blueprintInstanceResolver,
            ldtkProjectResolver)
        {
            IsDirty = true
        };

    public static SceneDocument Open(
        string path,
        SelectionService selection,
        Action<string, Exception?>? reportError = null,
        Func<BlueprintInstanceReference, EntityBlueprint>? blueprintInstanceResolver = null,
        Func<LDtkSceneReference, LDtkFile>? ldtkProjectResolver = null)
    {
        var fullPath = System.IO.Path.GetFullPath(path);
        var source = SceneDocumentSerializer.Deserialize(File.ReadAllText(fullPath));
        return new SceneDocument(
            source,
            fullPath,
            selection,
            reportError,
            blueprintInstanceResolver,
            ldtkProjectResolver);
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
        if (!string.IsNullOrWhiteSpace(path))
            Path = System.IO.Path.GetFullPath(path);
        if (Path is null)
            throw new InvalidOperationException("Choose a path before saving this scene.");
        if (Scene is not null)
            CaptureSource();

        var directory = System.IO.Path.GetDirectoryName(Path)!;
        Directory.CreateDirectory(directory);
        var temporaryPath = Path + $".{Guid.NewGuid():N}.tmp";
        try
        {
            File.WriteAllText(temporaryPath, SceneDocumentSerializer.Serialize(_source));
            File.Move(temporaryPath, Path, true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }

        IsDirty = false;
    }

    public void Apply(string name, Action<EditorScene> mutation)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(mutation);
        var scene = Scene ?? throw new InvalidOperationException("The live scene is unavailable during reload.");
        var before = CaptureJson();
        var beforeSelection = Selection.EntityIds.ToArray();
        var wasDirty = IsDirty;

        mutation(scene);
        scene.FlushStructuralChanges();
        Selection.RemoveMissing(scene);
        var after = CaptureJson();
        var afterSelection = Selection.EntityIds.ToArray();
        if (string.Equals(before, after, StringComparison.Ordinal))
            return;

        IsDirty = true;
        _lastChangeTimestamp = Stopwatch.GetTimestamp();
        Undo.Record(new SceneSnapshotCommand(
            name,
            this,
            before,
            after,
            beforeSelection,
            afterSelection,
            wasDirty,
            true));
    }

    public SceneEditTransaction BeginTransaction(string name)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (Scene is null)
            throw new InvalidOperationException("The live scene is unavailable during reload.");
        return new SceneEditTransaction(
            this,
            name,
            CaptureJson(),
            Selection.EntityIds.ToArray(),
            IsDirty);
    }

    public Entity CreateEmpty(string name = "Entity", Entity? parent = null)
    {
        if (parent?.IsLDtkGenerated == true)
            throw new InvalidOperationException("LDtk-generated entities cannot own Dreambit-authored children.");
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
            RecordLDtkEntityName(entity);
        });
    }

    public Entity Duplicate(Entity entity)
    {
        if (entity.IsLDtkGenerated)
            throw new InvalidOperationException("LDtk-generated entities are recreated from their source and cannot be duplicated.");
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
        if (parent?.IsLDtkGenerated == true)
            throw new InvalidOperationException("LDtk-generated entities cannot own Dreambit-authored children.");
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
        if (!TryGetBlueprintInstanceRoot(entity, out var root, out _))
            return;
        Apply("Unbox Blueprint Instance", _ =>
        {
            var source = FindSourceEntity(root.Id)
                         ?? throw new InvalidOperationException("The Blueprint instance source was not found.");
            source.BlueprintInstance = null;
        });
    }

    public void Delete(IEnumerable<Entity> entities)
    {
        var roots = RemoveDescendantDuplicates(entities).ToArray();
        if (roots.Length == 0)
            return;
        if (roots.Any(entity => entity.IsLDtkGenerated))
            throw new InvalidOperationException(
                "LDtk-generated entities are recreated from their source. Disable their import option instead of deleting them.");
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
        if (entity.IsLDtkGenerated || parent?.IsLDtkGenerated == true)
            throw new InvalidOperationException("LDtk-generated hierarchy structure is owned by the LDtk source.");
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

    public void RecordLDtkEntityName(Entity entity)
    {
        if (TryGetLDtkOverride(entity, out var entityOverride))
            entityOverride.Name = entity.Name;
    }

    public void RecordLDtkEntityEnabled(Entity entity)
    {
        if (TryGetLDtkOverride(entity, out var entityOverride))
            entityOverride.Enabled = entity.LocallyEnabled;
    }

    public void RecordLDtkPosition(Entity entity)
    {
        if (TryGetLDtkOverride(entity, out var entityOverride))
            entityOverride.Position = entity.Transform.Position;
    }

    public void RecordLDtkRotation(Entity entity)
    {
        if (TryGetLDtkOverride(entity, out var entityOverride))
            entityOverride.Rotation2D = entity.Transform.Rotation2D;
    }

    public void RecordLDtkScale(Entity entity)
    {
        if (TryGetLDtkOverride(entity, out var entityOverride))
            entityOverride.Scale = entity.Transform.Scale;
    }

    public void RecordLDtkComponentMember(Component component, string memberName, object? value)
    {
        if (!TryGetLDtkOverride(component.Entity, out var entityOverride))
            return;
        var componentType = component.GetType();
        var componentKey = componentType.FullName ?? componentType.AssemblyQualifiedName ?? componentType.Name;
        if (!entityOverride.Components.TryGetValue(componentKey, out var properties))
        {
            properties = new Dictionary<string, JToken>(StringComparer.OrdinalIgnoreCase);
            entityOverride.Components[componentKey] = properties;
        }
        properties[memberName] = DreambitJson.ToToken(value);
    }

    public void UpdateLDtkImportOptions(string name, Action<LDtkImportOptions> mutation)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(mutation);
        var reference = _source.LDtk
                        ?? throw new InvalidOperationException("This scene is not linked to an LDtk project.");
        var before = CaptureJson();
        var beforeSelection = Selection.EntityIds.ToArray();
        var wasDirty = IsDirty;
        var updated = (reference.ImportOptions ?? new LDtkImportOptions()).Clone();
        mutation(updated);
        updated.Validate();
        reference.ImportOptions = updated;
        try
        {
            RebuildPreservingSelection();
        }
        catch
        {
            _source = SceneDocumentSerializer.Deserialize(before);
            throw;
        }
        var after = CaptureJson();
        if (string.Equals(before, after, StringComparison.Ordinal))
            return;

        IsDirty = true;
        _lastChangeTimestamp = Stopwatch.GetTimestamp();
        Undo.Record(new SceneSnapshotCommand(
            name,
            this,
            before,
            after,
            beforeSelection,
            Selection.EntityIds.ToArray(),
            wasDirty,
            true));
    }

    /// <summary>Reloads the linked LDtk source while preserving Dreambit-authored scene entities.</summary>
    public void ReimportLDtk()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_source.LDtk is null || Scene is null)
            return;
        RebuildPreservingSelection();
    }

    public void BeforeAssemblyReload()
    {
        if (Scene is null)
            return;
        CaptureSource();
        Scene.Dispose();
        Scene = null;
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
        var selection = Selection.EntityIds.ToArray();
        CaptureSource();
        Scene.Dispose();
        Scene = null;
        RebuildLiveScene();
        Selection.Restore(selection);
        Selection.RemoveMissing(Scene);
    }

    private void RebuildPreservingSelection()
    {
        var selected = Selection.Resolve(Scene)
            .Select(entity => new SelectionMarker(entity.Id, entity.LDtkSourceKey))
            .ToArray();
        CaptureSource();
        var previous = Scene!;
        var replacement = BuildLiveScene();
        Scene = replacement;
        previous.Dispose();
        var restored = selected.Select(marker =>
        {
            if (string.IsNullOrWhiteSpace(marker.LDtkSourceKey))
                return marker.EntityId;
            return Scene!.GetAllEntities()
                .FirstOrDefault(entity => entity.LDtkSourceKey == marker.LDtkSourceKey)?.Id ?? Guid.Empty;
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

    private void CaptureSource()
    {
        _source = SceneDocumentSerializer.Capture(
            Scene!,
            _source,
            Name,
            _explicitlyClearedReferences);
        _explicitlyClearedReferences.Clear();
    }

    private void Restore(string json, IReadOnlyList<Guid> selection, bool dirty)
    {
        _source = SceneDocumentSerializer.Deserialize(json);
        Scene?.Dispose();
        Scene = null;
        RebuildLiveScene();
        Selection.Restore(selection);
        Selection.RemoveMissing(Scene);
        IsDirty = dirty;
        _lastChangeTimestamp = Stopwatch.GetTimestamp();
    }

    private void RebuildLiveScene()
    {
        Scene = BuildLiveScene();
        Selection.RemoveMissing(Scene);
    }

    private EditorScene BuildLiveScene()
    {
        var scene = new EditorScene();
        try
        {
            scene.LoadIntoSelf(_source, CreateEditorLoadOptions());
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

    private SceneBlueprintLoadOptions CreateEditorLoadOptions() => new()
    {
        AllowMissingComponentTypes = true,
        PreserveEntityIds = true,
        TolerateComponentLoadErrors = true,
        BlueprintInstanceResolver = _blueprintInstanceResolver,
        LDtkProjectResolver = _ldtkProjectResolver,
        MarkImportedLDtkEntitiesEditorOnly = true,
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
        Scene?.Dispose();
        Scene = null;
        Undo.Clear();
        _disposed = true;
    }

    private sealed record SceneSnapshotCommand(
        string Name,
        SceneDocument Document,
        string Before,
        string After,
        IReadOnlyList<Guid> BeforeSelection,
        IReadOnlyList<Guid> AfterSelection,
        bool BeforeDirty,
        bool AfterDirty) : IUndoableEditorCommand
    {
        public void Undo() => Document.Restore(Before, BeforeSelection, BeforeDirty);
        public void Redo() => Document.Restore(After, AfterSelection, AfterDirty);
    }

    private readonly record struct SelectionMarker(Guid EntityId, string? LDtkSourceKey);

    internal sealed class SceneEditTransaction : IDisposable
    {
        private readonly SceneDocument _document;
        private readonly string _name;
        private readonly string _before;
        private readonly IReadOnlyList<Guid> _beforeSelection;
        private readonly bool _beforeDirty;
        private bool _finished;

        internal SceneEditTransaction(
            SceneDocument document,
            string name,
            string before,
            IReadOnlyList<Guid> beforeSelection,
            bool beforeDirty)
        {
            _document = document;
            _name = name;
            _before = before;
            _beforeSelection = beforeSelection;
            _beforeDirty = beforeDirty;
        }

        public void Update(Action<EditorScene> mutation)
        {
            if (_finished)
                throw new InvalidOperationException("The scene edit transaction has finished.");
            mutation(_document.Scene ?? throw new InvalidOperationException("The live scene is unavailable."));
            _document.Scene.FlushStructuralChanges();
            _document.IsDirty = true;
            _document._lastChangeTimestamp = Stopwatch.GetTimestamp();
        }

        public void Commit()
        {
            if (_finished)
                return;
            var after = _document.CaptureJson();
            if (!string.Equals(_before, after, StringComparison.Ordinal))
            {
                _document.Undo.Record(new SceneSnapshotCommand(
                    _name,
                    _document,
                    _before,
                    after,
                    _beforeSelection,
                    _document.Selection.EntityIds.ToArray(),
                    _beforeDirty,
                    true));
            }
            _finished = true;
        }

        public void Cancel()
        {
            if (_finished)
                return;
            _document.Restore(_before, _beforeSelection, _beforeDirty);
            _finished = true;
        }

        public void Dispose() => Commit();
    }
}
