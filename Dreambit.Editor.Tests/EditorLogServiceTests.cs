using Dreambit.Editor.Logging;

namespace Dreambit.Editor.Tests;

public sealed class EditorLogServiceTests
{
    [Fact]
    public void EnforcesBoundedCapacity()
    {
        var logs = new EditorLogService(2);

        logs.Info("Test", "first");
        logs.Warning("Test", "second");
        logs.Error("Test", "third");

        var snapshot = logs.GetSnapshot();
        Assert.Collection(
            snapshot.Entries,
            entry => Assert.Equal("second", entry.Message),
            entry => Assert.Equal("third", entry.Message));
    }

    [Fact]
    public void ClearPublishesANewEmptySnapshot()
    {
        var logs = new EditorLogService();
        logs.Info("Test", "message");
        var populated = logs.GetSnapshot();

        logs.Clear();
        var cleared = logs.GetSnapshot();

        Assert.True(cleared.Version > populated.Version);
        Assert.Empty(cleared.Entries);
    }

    [Fact]
    public void ErrorRetainsExceptionDetails()
    {
        var logs = new EditorLogService();
        logs.Error("Extension", "Failed safely", new InvalidOperationException("boom"));

        var entry = Assert.Single(logs.GetSnapshot().Entries);
        Assert.Equal(EditorLogSeverity.Error, entry.Severity);
        Assert.Contains("InvalidOperationException", entry.Details);
        Assert.Contains("boom", entry.Details);
    }
}
