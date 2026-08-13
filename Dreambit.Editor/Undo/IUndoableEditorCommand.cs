namespace Dreambit.Editor.Undo;

internal interface IUndoableEditorCommand
{
    string Name { get; }
    string? MergeKey => null;
    bool IsNoOp => false;

    /// <summary>
    /// Extends this command to include a subsequent edit from the same active
    /// interaction. Implementations retain their original undo state and adopt
    /// only the subsequent command's redo state.
    /// </summary>
    bool TryMerge(IUndoableEditorCommand subsequent) => false;

    void Undo();
    void Redo();
}
