using System.Diagnostics;

namespace Dreambit.Editor.Projects;

internal sealed record ProcessCommand(
    string FileName,
    IReadOnlyList<string> Arguments,
    string WorkingDirectory);

internal sealed record ProcessRunResult(
    int ExitCode,
    IReadOnlyList<string> Output)
{
    public bool Succeeded => ExitCode == 0;
}

internal interface IProcessRunner
{
    Task<ProcessRunResult> RunAsync(
        ProcessCommand command,
        Action<string>? output,
        CancellationToken cancellationToken);
}

internal sealed class ProcessRunner : IProcessRunner
{
    public async Task<ProcessRunResult> RunAsync(
        ProcessCommand command,
        Action<string>? output,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command.FileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.WorkingDirectory);

        var startInfo = new ProcessStartInfo
        {
            FileName = command.FileName,
            WorkingDirectory = command.WorkingDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        foreach (var argument in command.Arguments)
            startInfo.ArgumentList.Add(argument);

        using var process = new Process { StartInfo = startInfo };
        try
        {
            if (!process.Start())
                throw new InvalidOperationException($"Could not start '{command.FileName}'.");
        }
        catch (Exception exception) when (
            exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return new ProcessRunResult(-1, [$"Could not start '{command.FileName}'. {exception.Message}"]);
        }

        var lines = new List<string>();
        var sync = new object();
        var standardOutput = ReadLinesAsync(
            process.StandardOutput,
            lines,
            sync,
            output,
            cancellationToken);
        var standardError = ReadLinesAsync(
            process.StandardError,
            lines,
            sync,
            output,
            cancellationToken);

        try
        {
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            await Task.WhenAll(standardOutput, standardError).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            try
            {
                await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
                await Task.WhenAll(standardOutput, standardError).ConfigureAwait(false);
            }
            catch (Exception exception) when (
                exception is OperationCanceledException or IOException or ObjectDisposedException)
            {
            }
            throw;
        }

        lock (sync)
            return new ProcessRunResult(process.ExitCode, lines.ToArray());
    }

    private static async Task ReadLinesAsync(
        StreamReader reader,
        ICollection<string> lines,
        object sync,
        Action<string>? output,
        CancellationToken cancellationToken)
    {
        while (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } line)
        {
            lock (sync)
                lines.Add(line);
            output?.Invoke(line);
        }
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
        }
        catch (System.ComponentModel.Win32Exception)
        {
        }
    }
}
