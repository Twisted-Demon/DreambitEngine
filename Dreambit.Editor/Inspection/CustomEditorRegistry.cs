using Dreambit.Editor.Compilation;
using Dreambit.EditorApi;

namespace Dreambit.Editor.Inspection;

internal sealed class CustomEditorRegistry : IDisposable
{
    private readonly GameAssemblyLoadService _assemblies;
    private readonly List<Entry> _entries = [];
    private readonly Action<string, Exception?>? _reportError;
    private bool _disposed;

    public CustomEditorRegistry(
        GameAssemblyLoadService assemblies,
        Action<string, Exception?>? reportError = null)
    {
        _assemblies = assemblies;
        _reportError = reportError;
        _assemblies.Reloading += OnReloading;
        _assemblies.Reloaded += OnReloaded;
        if (_assemblies.Current is { } current)
            Rebuild(current);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _assemblies.Reloading -= OnReloading;
        _assemblies.Reloaded -= OnReloaded;
        Clear();
        _disposed = true;
    }

    public bool TryGet(Type targetType, out IDreambitCustomEditor? editor)
    {
        foreach (var entry in _entries)
            if (entry.TargetType == targetType ||
                (entry.IncludeDerivedTypes && entry.TargetType.IsAssignableFrom(targetType)))
            {
                editor = entry.Editor;
                return true;
            }

        editor = null;
        return false;
    }

    private void OnReloading(LoadedGameAssembly? _)
    {
        Clear();
    }

    private void OnReloaded(LoadedGameAssembly assembly)
    {
        Rebuild(assembly);
    }

    private void Rebuild(LoadedGameAssembly assembly)
    {
        Clear();
        foreach (var editorType in assembly.Types.CustomEditorTypes)
        {
            var attributes = editorType
                .GetCustomAttributes(typeof(DreambitCustomEditorAttribute), false)
                .OfType<DreambitCustomEditorAttribute>()
                .ToArray();
            if (attributes.Length == 0)
                continue;
            try
            {
                var editor = (IDreambitCustomEditor?)Activator.CreateInstance(editorType)
                             ?? throw new InvalidOperationException("Custom Editor constructor returned null.");
                foreach (var attribute in attributes)
                {
                    if (_entries.Any(entry => entry.TargetType == attribute.TargetType))
                        throw new InvalidOperationException(
                            $"A custom Editor is already registered for '{attribute.TargetType.FullName}'.");
                    _entries.Add(new Entry(
                        attribute.TargetType,
                        attribute.IncludeDerivedTypes,
                        editor));
                }
            }
            catch (Exception exception)
            {
                _reportError?.Invoke($"Could not load custom Editor '{editorType.FullName}'.", exception);
            }
        }
    }

    private void Clear()
    {
        foreach (var editor in _entries.Select(entry => entry.Editor).Distinct())
            if (editor is IDisposable disposable)
                disposable.Dispose();
        _entries.Clear();
    }

    private sealed record Entry(
        Type TargetType,
        bool IncludeDerivedTypes,
        IDreambitCustomEditor Editor);
}
