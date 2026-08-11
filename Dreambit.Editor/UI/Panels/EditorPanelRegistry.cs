using Dreambit.Editor.Persistence;
using ImGuiNET;

namespace Dreambit.Editor.UI.Panels;

internal sealed class EditorPanelRegistry : IDisposable
{
    private readonly List<IEditorPanel> _panels = [];
    private readonly Dictionary<string, IEditorPanel> _panelsById =
        new(StringComparer.Ordinal);
    private readonly EditorWorkspaceState _workspaceState;
    private bool _disposed;

    public EditorPanelRegistry(EditorWorkspaceState workspaceState)
    {
        _workspaceState = workspaceState;
    }

    public IReadOnlyList<IEditorPanel> Panels => _panels;

    public void Register(IEditorPanel panel)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(panel);

        if (!_panelsById.TryAdd(panel.Id, panel))
            throw new InvalidOperationException($"Panel id '{panel.Id}' is already registered.");

        if (_workspaceState.PanelVisibility.TryGetValue(panel.Id, out var isOpen))
            panel.IsOpen = isOpen;

        _panels.Add(panel);
    }

    public void DrawPanels()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        foreach (var panel in _panels)
        {
            var wasOpen = panel.IsOpen;
            panel.Draw();
            if (wasOpen != panel.IsOpen)
                _workspaceState.PanelVisibility[panel.Id] = panel.IsOpen;
        }
    }

    public void DrawWindowMenu()
    {
        foreach (var panel in _panels)
        {
            var isOpen = panel.IsOpen;
            if (!ImGui.MenuItem(panel.Title, string.Empty, isOpen))
                continue;

            panel.IsOpen = !isOpen;
            _workspaceState.PanelVisibility[panel.Id] = panel.IsOpen;
        }
    }

    public void OpenAll()
    {
        foreach (var panel in _panels)
        {
            panel.IsOpen = true;
            _workspaceState.PanelVisibility[panel.Id] = true;
        }
    }

    public IEditorPanel GetRequired(string id) =>
        _panelsById.TryGetValue(id, out var panel)
            ? panel
            : throw new KeyNotFoundException($"Panel '{id}' is not registered.");

    public void Dispose()
    {
        if (_disposed)
            return;

        for (var index = _panels.Count - 1; index >= 0; index--)
            _panels[index].Dispose();

        _panels.Clear();
        _panelsById.Clear();
        _disposed = true;
    }
}
