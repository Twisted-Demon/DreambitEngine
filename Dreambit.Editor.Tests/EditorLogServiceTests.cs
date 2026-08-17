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

    [Fact]
    public void ErrorsArePersistedWithoutPersistingLowerSeverityEntries()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            nameof(EditorLogServiceTests),
            Guid.NewGuid().ToString("N"));
        var path = Path.Combine(root, "errors.log");
        try
        {
            var logs = new EditorLogService(errorLogPath: path);

            logs.Warning("Test", "recoverable warning");
            logs.Error("Viewport", "render failed", new InvalidOperationException("boom"));

            var persisted = File.ReadAllText(path);
            Assert.Contains("ERROR [Viewport] render failed", persisted);
            Assert.Contains("InvalidOperationException", persisted);
            Assert.Contains("boom", persisted);
            Assert.DoesNotContain("recoverable warning", persisted);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }
}
