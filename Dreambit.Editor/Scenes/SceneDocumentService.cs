using Dreambit.Editor.Compilation;
using Dreambit.Editor.Projects;

namespace Dreambit.Editor.Scenes;

internal sealed class SceneDocumentService : IDisposable
{
    private readonly DreambitProjectDefinition _project;
    private readonly GameAssemblyLoadService _assemblies;
    private readonly Action<string, Exception?>? _reportError;
    private bool _disposed;

    public SceneDocumentService(
        DreambitProjectDefinition project,
        GameAssemblyLoadService assemblies,
        Action<string, Exception?>? reportError = null)
    {
        _project = project;
        _assemblies = assemblies;
        _reportError = reportError;
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
        Current = SceneDocument.CreateNew(name, Selection, _reportError);
        CurrentChanged?.Invoke(Current);
        return Current;
    }

    public SceneDocument Open(string path)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var fullPath = ResolveScenePath(path);
        Close();
        Current = SceneDocument.Open(fullPath, Selection, _reportError);
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

    public void Update(bool autoSave, TimeSpan autoSaveDelay) =>
        Current?.Update(autoSave, autoSaveDelay);

    public void ReloadContent() => Current?.ReloadContent();

    public void Close()
    {
        Current?.Dispose();
        Current = null;
        Selection.Clear();
        CurrentChanged?.Invoke(null);
    }

    private void OnAssemblyReloading(LoadedGameAssembly? _) => Current?.BeforeAssemblyReload();
    private void OnAssemblyReloaded(LoadedGameAssembly _) => Current?.AfterAssemblyReload();

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
