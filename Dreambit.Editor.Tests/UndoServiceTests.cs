using Dreambit.Editor.Undo;

namespace Dreambit.Editor.Tests;

public sealed class UndoServiceTests
{
    [Fact]
    public void FailedUndoLeavesTheCommandOnTheUndoStack()
    {
        var service = new UndoService();
        service.Record(new TestCommand("Failing Undo", undo: () => throw new InvalidOperationException("undo")));

        Assert.Throws<InvalidOperationException>(() => service.Undo());

        Assert.True(service.CanUndo);
        Assert.False(service.CanRedo);
        Assert.Equal("Failing Undo", service.UndoName);
    }

    [Fact]
    public void FailedRedoLeavesTheCommandOnTheRedoStack()
    {
        var service = new UndoService();
        service.Record(new TestCommand("Failing Redo", redo: () => throw new InvalidOperationException("redo")));
        Assert.True(service.Undo());

        Assert.Throws<InvalidOperationException>(() => service.Redo());

        Assert.False(service.CanUndo);
        Assert.True(service.CanRedo);
        Assert.Equal("Failing Redo", service.RedoName);
    }

    [Fact]
    public void SameKeyCommandsMergeAndUndoRestoresBeforeFirstEdit()
    {
        var value = 1;
        var service = new UndoService();
        service.Record(new ValueCommand("Change Value", "Value", 0, 1, result => value = result));
        value = 2;
        service.Record(new ValueCommand("Change Value", "Value", 1, 2, result => value = result));
        value = 3;
        service.Record(new ValueCommand("Change Value", "Value", 2, 3, result => value = result));

        Assert.True(service.Undo());
        Assert.Equal(0, value);
        Assert.False(service.CanUndo);

        Assert.True(service.Redo());
        Assert.Equal(3, value);
    }

    [Fact]
    public void DifferentMergeKeysRemainSeparateCommands()
    {
        var value = 2;
        var service = new UndoService();
        service.Record(new ValueCommand("First", "First", 0, 1, result => value = result));
        service.Record(new ValueCommand("Second", "Second", 1, 2, result => value = result));

        Assert.True(service.Undo());
        Assert.Equal(1, value);
        Assert.True(service.CanUndo);
        Assert.True(service.Undo());
        Assert.Equal(0, value);
    }

    [Fact]
    public void EndingMergeGroupSeparatesLaterCommandsWithTheSameKey()
    {
        var value = 2;
        var service = new UndoService();
        service.Record(new ValueCommand("First Drag", "Value", 0, 1, result => value = result));
        service.EndMergeGroup();
        service.Record(new ValueCommand("Second Drag", "Value", 1, 2, result => value = result));

        Assert.True(service.Undo());
        Assert.Equal(1, value);
        Assert.True(service.CanUndo);
        Assert.True(service.Undo());
        Assert.Equal(0, value);
    }

    private sealed record TestCommand(
        string Name,
        Action? undo = null,
        Action? redo = null) : IUndoableEditorCommand
    {
        public void Undo() => undo?.Invoke();
        public void Redo() => redo?.Invoke();
    }

    private sealed class ValueCommand(
        string name,
        string mergeKey,
        int before,
        int after,
        Action<int> apply) : IUndoableEditorCommand
    {
        public string Name { get; } = name;
        public string? MergeKey { get; } = mergeKey;
        public bool IsNoOp => before == _after;
        private int _after = after;

        public bool TryMerge(IUndoableEditorCommand subsequent)
        {
            if (subsequent is not ValueCommand next ||
                !string.Equals(MergeKey, next.MergeKey, StringComparison.Ordinal))
            {
                return false;
            }

            _after = next._after;
            return true;
        }

        public void Undo() => apply(before);
        public void Redo() => apply(_after);
    }
}
