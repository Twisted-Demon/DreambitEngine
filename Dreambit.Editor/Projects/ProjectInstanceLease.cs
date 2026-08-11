using System.Text;

namespace Dreambit.Editor.Projects;

internal sealed class ProjectInstanceLease : IDisposable
{
    private readonly FileStream _stream;
    private bool _disposed;

    private ProjectInstanceLease(FileStream stream)
    {
        _stream = stream;
    }

    public static bool TryAcquire(
        string lockPath,
        string projectRoot,
        out ProjectInstanceLease? lease,
        out string? error)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(lockPath)!);
            var stream = new FileStream(
                lockPath,
                FileMode.OpenOrCreate,
                FileAccess.ReadWrite,
                FileShare.None);

            stream.SetLength(0);
            using (var writer = new StreamWriter(
                       stream,
                       new UTF8Encoding(false),
                       leaveOpen: true))
            {
                writer.WriteLine($"processId={Environment.ProcessId}");
                writer.WriteLine($"project={projectRoot}");
                writer.WriteLine($"openedUtc={DateTimeOffset.UtcNow:O}");
                writer.Flush();
            }

            stream.Flush(true);
            stream.Position = 0;
            lease = new ProjectInstanceLease(stream);
            error = null;
            return true;
        }
        catch (IOException)
        {
            lease = null;
            error = $"The project '{projectRoot}' is already open in another Dreambit Editor process.";
            return false;
        }
        catch (UnauthorizedAccessException exception)
        {
            lease = null;
            error = $"Could not create the project session lock. {exception.Message}";
            return false;
        }
    }

    public static bool IsAvailable(string lockPath)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(lockPath)!);
            using var stream = new FileStream(
                lockPath,
                FileMode.OpenOrCreate,
                FileAccess.ReadWrite,
                FileShare.None);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _stream.Dispose();
        _disposed = true;
    }
}
