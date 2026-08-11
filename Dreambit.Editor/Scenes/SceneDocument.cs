using System.Diagnostics;
using Dreambit.ECS;
using Dreambit.Editor.Undo;

namespace Dreambit.Editor.Scenes;

internal sealed class SceneDocument : IDisposable
{
    private readonly Action<string, Exception?>? _reportError;
    private SceneBlueprint _source;
    private readonly HashSet<string> _explicitlyClearedReferences = new(StringComparer.Ordinal);
    private long _lastChangeTimestamp;
    private bool _disposed;

    public SceneDocument(
        SceneBlueprint source,
        string? path,
        SelectionService selection,
        Action<string, Exception?>? reportError = null)
    {
        _source = source;
        Path = path;
        Selection = selection;
        Undo = new UndoService();
        _reportError = reportError;
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

    public static SceneDocument CreateNew(
        string name,
        SelectionService selection,
        Action<string, Exception?>? reportError = null) =>
        new(new SceneBlueprint { Name = name, Entities = [] }, null, selection, reportError)
        {
            IsDirty = true
        };

    public static SceneDocument Open(
        string path,
        SelectionService selection,
        Action<string, Exception?>? reportError = null)
    {
        var fullPath = System.IO.Path.GetFullPath(path);
        var source = SceneDocumentSerializer.Deserialize(File.ReadAllText(fullPath));
        return new SceneDocument(source, fullPath, selection, reportError);
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
        var trimmed = name.Trim();
        if (trimmed.Length == 0 || string.Equals(entity.Name, trimmed, StringComparison.Ordinal))
            return;
        Apply("Rename Entity", _ => entity.Name = trimmed);
    }

    public Entity Duplicate(Entity entity)
    {
        Entity? duplicated = null;
        Apply("Duplicate Entity", scene =>
        {
            var captured = SceneDocumentSerializer.CaptureSubtree(scene, _source, entity);
            var clone = SceneDocumentSerializer.CloneAndRemap(captured);
            scene.LoadIntoSelf(
                new SceneBlueprint { Name = Name, Entities = [clone] },
                SceneBlueprintLoadOptions.Editor);
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
        Entity? created = null;
        Apply("Instantiate Blueprint", scene =>
        {
            var clone = SceneDocumentSerializer.CloneAndRemap(blueprint);
            scene.LoadIntoSelf(
                new SceneBlueprint { Name = Name, Entities = [clone] },
                SceneBlueprintLoadOptions.Editor);
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

    public void Delete(IEnumerable<Entity> entities)
    {
        var roots = RemoveDescendantDuplicates(entities).ToArray();
        if (roots.Length == 0)
            return;

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
        Apply("Reparent Entity", _ => entity.SetParent(parent, preserveWorldTransform));
    }

    public void MarkReferenceCleared(Entity entity, Type componentType, string memberName)
    {
        _explicitlyClearedReferences.Add(
            SceneDocumentSerializer.GetReferenceKey(entity.Id, componentType, memberName));
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
        var scene = new EditorScene();
        try
        {
            scene.LoadIntoSelf(_source, SceneBlueprintLoadOptions.Editor);
            scene.FlushStructuralChanges();
            Scene = scene;
            Selection.RemoveMissing(scene);
        }
        catch
        {
            scene.Dispose();
            throw;
        }
    }

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
