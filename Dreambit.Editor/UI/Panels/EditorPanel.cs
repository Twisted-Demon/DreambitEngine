using ImGuiNET;

namespace Dreambit.Editor.UI.Panels;

internal abstract class EditorPanel : IEditorPanel
{
    private bool _disposed;

    protected EditorPanel(string id, string title, bool isOpenByDefault = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        Id = id;
        Title = title;
        WindowName = $"{title}##{id}";
        IsOpen = isOpenByDefault;
    }

    public string Id { get; }
    public string Title { get; }
    public string WindowName { get; }
    public bool IsOpen { get; set; }
    public virtual bool IsAvailable => true;

    protected virtual ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.None;

    public void Draw()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!IsOpen || !IsAvailable)
            return;

        var isOpen = IsOpen;
        var visible = ImGui.Begin(WindowName, ref isOpen, WindowFlags);
        IsOpen = isOpen;

        try
        {
            if (visible)
                DrawContents();
        }
        finally
        {
            ImGui.End();
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        try
        {
            DisposeCore();
        }
        finally
        {
            _disposed = true;
        }
    }

    protected abstract void DrawContents();

    protected virtual void DisposeCore()
    {
    }
}
