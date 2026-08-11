namespace Dreambit.Editor.Undo;

internal sealed class UndoService
{
    private readonly List<IUndoableEditorCommand> _undo = [];
    private readonly List<IUndoableEditorCommand> _redo = [];

    public event Action? Changed;

    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;
    public string? UndoName => CanUndo ? _undo[^1].Name : null;
    public string? RedoName => CanRedo ? _redo[^1].Name : null;

    public void Record(IUndoableEditorCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        _undo.Add(command);
        _redo.Clear();
        Changed?.Invoke();
    }

    public bool Undo()
    {
        if (!CanUndo)
            return false;

        var command = _undo[^1];
        _undo.RemoveAt(_undo.Count - 1);
        command.Undo();
        _redo.Add(command);
        Changed?.Invoke();
        return true;
    }

    public bool Redo()
    {
        if (!CanRedo)
            return false;

        var command = _redo[^1];
        _redo.RemoveAt(_redo.Count - 1);
        command.Redo();
        _undo.Add(command);
        Changed?.Invoke();
        return true;
    }

    public void Clear()
    {
        _undo.Clear();
        _redo.Clear();
        Changed?.Invoke();
    }
}
