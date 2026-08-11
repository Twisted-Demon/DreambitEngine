namespace Dreambit.Editor.Undo;

internal interface IUndoableEditorCommand
{
    string Name { get; }
    void Undo();
    void Redo();
}
