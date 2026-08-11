using System.Text.Json;

namespace Dreambit.Editor.Projects;

internal sealed class DreambitProjectMetadataStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public bool TryLoad(
        string metadataPath,
        out DreambitProjectMetadata? metadata,
        out ProjectDiagnostic? diagnostic)
    {
        try
        {
            using var stream = File.OpenRead(metadataPath);
            metadata = JsonSerializer.Deserialize<DreambitProjectMetadata>(
                stream,
                SerializerOptions);

            if (metadata is null)
            {
                diagnostic = new ProjectDiagnostic(
                    ProjectDiagnosticSeverity.Error,
                    "DBP002",
                    "The Dreambit project metadata file is empty.",
                    metadataPath);
                return false;
            }

            diagnostic = null;
            return true;
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or JsonException)
        {
            metadata = null;
            diagnostic = new ProjectDiagnostic(
                ProjectDiagnosticSeverity.Error,
                "DBP003",
                $"Could not read Dreambit project metadata. {exception.Message}",
                metadataPath);
            return false;
        }
    }

    public bool TrySave(
        string projectRoot,
        DreambitProjectMetadata metadata,
        out string? error)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        var metadataPath = Path.Combine(
            projectRoot,
            DreambitProjectMetadata.RelativePath.Replace('/', Path.DirectorySeparatorChar));
        var directory = Path.GetDirectoryName(metadataPath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            error = $"Metadata path '{metadataPath}' has no parent directory.";
            return false;
        }

        var temporaryPath = Path.Combine(
            directory,
            $".{Path.GetFileName(metadataPath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            Directory.CreateDirectory(directory);
            using (var stream = new FileStream(
                       temporaryPath,
                       FileMode.CreateNew,
                       FileAccess.Write,
                       FileShare.None))
            {
                JsonSerializer.Serialize(stream, metadata, SerializerOptions);
                stream.Flush(true);
            }

            File.Move(temporaryPath, metadataPath, true);
            error = null;
            return true;
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException)
        {
            error = $"Could not save Dreambit project metadata. {exception.Message}";
            return false;
        }
        finally
        {
            TryDeleteTemporaryFile(temporaryPath);
        }
    }

    private static void TryDeleteTemporaryFile(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
