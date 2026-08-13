namespace Dreambit.Editor.Undo;

internal sealed class UndoService
{
    private readonly List<IUndoableEditorCommand> _undo = [];
    private readonly List<IUndoableEditorCommand> _redo = [];
    private IUndoableEditorCommand? _mergeTarget;

    public event Action? Changed;

    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;
    public string? UndoName => CanUndo ? _undo[^1].Name : null;
    public string? RedoName => CanRedo ? _redo[^1].Name : null;

    public void Record(IUndoableEditorCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        var merged =
            _mergeTarget is not null &&
            command.MergeKey is not null &&
            _undo.Count > 0 &&
            ReferenceEquals(_undo[^1], _mergeTarget) &&
            string.Equals(_mergeTarget.MergeKey, command.MergeKey, StringComparison.Ordinal) &&
            _mergeTarget.TryMerge(command);

        if (merged)
        {
            if (_mergeTarget!.IsNoOp)
            {
                _undo.RemoveAt(_undo.Count - 1);
                _mergeTarget = null;
            }
        }
        else
        {
            _undo.Add(command);
            _mergeTarget = command.MergeKey is null ? null : command;
        }

        _redo.Clear();
        Changed?.Invoke();
    }

    /// <summary>
    /// Prevents the next command from merging with the current undo entry. UI
    /// code calls this when the active continuous interaction has ended.
    /// </summary>
    public void EndMergeGroup() => _mergeTarget = null;

    public bool Undo()
    {
        if (!CanUndo)
            return false;

        EndMergeGroup();
        var command = _undo[^1];
        // Execute before moving the command between stacks. A failed command must
        // remain available to retry (or diagnose) and must not appear redoable.
        command.Undo();
        _undo.RemoveAt(_undo.Count - 1);
        _redo.Add(command);
        Changed?.Invoke();
        return true;
    }

    public bool Redo()
    {
        if (!CanRedo)
            return false;

        EndMergeGroup();
        var command = _redo[^1];
        // Keep the redo stack intact until the command has completed successfully.
        command.Redo();
        _redo.RemoveAt(_redo.Count - 1);
        _undo.Add(command);
        Changed?.Invoke();
        return true;
    }

    public void Clear()
    {
        _undo.Clear();
        _redo.Clear();
        EndMergeGroup();
        Changed?.Invoke();
    }
}
