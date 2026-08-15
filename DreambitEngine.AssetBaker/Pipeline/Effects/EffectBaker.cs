using System.Buffers.Binary;
using System.Diagnostics;
using DreambitEngine.AssetBaker.Abstractions;
using Microsoft.Xna.Framework.Content.Pipeline;
using MonoGame.Framework.Content.Pipeline.Builder;

namespace DreambitEngine.AssetBaker.Pipeline.Effects;

/// <summary>Compiles MonoGame .fx source and stores raw MGFX bytecode in an FXB envelope.</summary>
public sealed class EffectBaker : AssetBakerBase
{
    // MonoGame's in-process content builder temporarily mutates shared importer metadata.
    // Serialize parallel bake requests while leaving the rest of the pipeline concurrent.
    private static readonly object ContentBuilderLock = new();

    public override string AssetTypeName => "effect";
    public override string[] SupportedInputs => [".fx"];
    public override string OutputExtension => ".fxb";

    public override void Bake(BakeContext ctx)
    {
        var blob = BakeToBytes(ctx);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(ctx.OutputPath))!);
        File.WriteAllBytes(ctx.OutputPath, blob.Data);
    }

    public override AssetBlob BakeToBytes(BakeContext ctx)
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable("DREAMBIT_EFFECT_WORKER"),
                "1",
                StringComparison.Ordinal))
        {
            return BakeInWorkerProcess(ctx);
        }

        return BakeInProcess(ctx);
    }

    private static AssetBlob BakeInWorkerProcess(BakeContext ctx)
    {
        var temporaryRoot = Path.Combine(Path.GetTempPath(), $"dreambit-effect-worker-{Guid.NewGuid():N}");
        var outputPath = Path.Combine(temporaryRoot, "effect.fxb");
        var runtimeConfigPath = Path.Combine(temporaryRoot, "effect-worker.runtimeconfig.json");
        Directory.CreateDirectory(temporaryRoot);
        try
        {
            var assemblyPath = typeof(EffectBaker).Assembly.Location;
            var dependencyContextPath = Path.ChangeExtension(assemblyPath, ".deps.json");
            if (!File.Exists(dependencyContextPath))
                throw new FileNotFoundException(
                    "The Dreambit effect compiler dependency manifest is missing.",
                    dependencyContextPath);
            File.WriteAllText(
                runtimeConfigPath,
                $$"""
                  {
                    "runtimeOptions": {
                      "tfm": "net{{Environment.Version.Major}}.{{Environment.Version.Minor}}",
                      "framework": {
                        "name": "Microsoft.NETCore.App",
                        "version": "{{Environment.Version.Major}}.{{Environment.Version.Minor}}.0"
                      }
                    }
                  }
                  """);
            var startInfo = new ProcessStartInfo("dotnet")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            startInfo.Environment["DREAMBIT_EFFECT_WORKER"] = "1";
            startInfo.ArgumentList.Add("exec");
            startInfo.ArgumentList.Add("--runtimeconfig");
            startInfo.ArgumentList.Add(runtimeConfigPath);
            startInfo.ArgumentList.Add("--depsfile");
            startInfo.ArgumentList.Add(dependencyContextPath);
            startInfo.ArgumentList.Add(assemblyPath);
            startInfo.ArgumentList.Add("__compile-effect");
            startInfo.ArgumentList.Add(Path.GetFullPath(ctx.InputPath));
            startInfo.ArgumentList.Add(outputPath);
            startInfo.ArgumentList.Add(string.IsNullOrWhiteSpace(ctx.LogicalRoot)
                ? Path.GetDirectoryName(Path.GetFullPath(ctx.InputPath))!
                : Path.GetFullPath(ctx.LogicalRoot));
            startInfo.ArgumentList.Add(NormalizePlatform(ctx.TargetPlatform));

            using var process = Process.Start(startInfo)
                                ?? throw new InvalidOperationException(
                                    "Could not start the Dreambit effect compiler.");
            var standardOutput = process.StandardOutput.ReadToEndAsync();
            var standardError = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit((int)TimeSpan.FromMinutes(2).TotalMilliseconds))
            {
                process.Kill(true);
                throw new TimeoutException(
                    $"Dreambit effect compiler timed out for '{ctx.InputPath}'.");
            }
            Task.WaitAll(standardOutput, standardError);
            if (process.ExitCode != 0 || !File.Exists(outputPath))
                throw new InvalidOperationException(
                    $"Dreambit effect compiler failed for '{ctx.InputPath}'. " +
                    string.Join(
                        Environment.NewLine,
                        standardError.Result.Trim(),
                        standardOutput.Result.Trim()).Trim());

            return new AssetBlob(
                GetLogicalPath(ctx, ".fxb"),
                AssetType.Effect,
                ".fxb",
                File.ReadAllBytes(outputPath));
        }
        finally
        {
            TryDeleteDirectory(temporaryRoot);
        }
    }

    private static AssetBlob BakeInProcess(BakeContext ctx)
    {
        var sourceRoot = string.IsNullOrWhiteSpace(ctx.LogicalRoot)
            ? Path.GetDirectoryName(Path.GetFullPath(ctx.InputPath))!
            : Path.GetFullPath(ctx.LogicalRoot);
        var relativePath = Path.GetRelativePath(sourceRoot, Path.GetFullPath(ctx.InputPath))
            .Replace('\\', '/');
        if (relativePath.StartsWith("../", StringComparison.Ordinal))
            throw new InvalidOperationException("Effect source must be inside its logical root.");

        var temporaryRoot = Path.Combine(Path.GetTempPath(), $"dreambit-effect-{Guid.NewGuid():N}");
        var outputRoot = Path.Combine(temporaryRoot, "out");
        var intermediateRoot = Path.Combine(temporaryRoot, "obj");
        Directory.CreateDirectory(outputRoot);
        Directory.CreateDirectory(intermediateRoot);
        try
        {
            var builder = new SingleEffectBuilder(relativePath);
            lock (ContentBuilderLock)
            {
                builder.Run([
                    "build",
                    "-p", NormalizePlatform(ctx.TargetPlatform),
                    "-s", sourceRoot,
                    "-o", outputRoot,
                    "-i", intermediateRoot
                ]);
                if (builder.FailedToBuild > 0)
                    throw new InvalidOperationException(
                        $"MonoGame failed to compile effect '{relativePath}'.");
            }

            var xnbPath = Directory.EnumerateFiles(outputRoot, "*.xnb", SearchOption.AllDirectories)
                .SingleOrDefault()
                ?? throw new InvalidDataException($"MonoGame did not produce compiled output for '{relativePath}'.");
            var (platform, effectCode) = ReadEffectCode(xnbPath);
            using var output = new MemoryStream(effectCode.Length + 16);
            output.Write("FXB0"u8);
            Span<byte> buffer = stackalloc byte[8];
            BinaryPrimitives.WriteUInt16LittleEndian(buffer[..2], 1);
            output.Write(buffer[..2]);
            output.WriteByte(platform);
            output.WriteByte(0);
            BinaryPrimitives.WriteUInt32LittleEndian(buffer[..4], checked((uint)effectCode.Length));
            output.Write(buffer[..4]);
            output.Write(effectCode);
            return new AssetBlob(
                GetLogicalPath(ctx, ".fxb"),
                AssetType.Effect,
                ".fxb",
                output.ToArray());
        }
        finally
        {
            TryDeleteDirectory(temporaryRoot);
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            Directory.Delete(path, true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static string NormalizePlatform(string platform) =>
        string.IsNullOrWhiteSpace(platform) ? "DesktopVK" : platform.Trim();

    private static (byte Platform, byte[] EffectCode) ReadEffectCode(string path)
    {
        using var stream = File.OpenRead(path);
        using var reader = new BinaryReader(stream);
        if (reader.ReadByte() != 'X' || reader.ReadByte() != 'N' || reader.ReadByte() != 'B')
            throw new InvalidDataException("Compiled effect is not an XNB document.");
        var platform = reader.ReadByte();
        var version = reader.ReadByte();
        var flags = reader.ReadByte();
        _ = reader.ReadUInt32();
        if (version != 5)
            throw new NotSupportedException($"Unsupported XNB version {version}.");
        if ((flags & 0xC0) != 0)
            throw new NotSupportedException("Compressed effect XNB output is not supported.");

        var readerCount = reader.Read7BitEncodedInt();
        for (var index = 0; index < readerCount; index++)
        {
            _ = reader.ReadString();
            _ = reader.ReadInt32();
        }
        _ = reader.Read7BitEncodedInt(); // shared resource count
        var typeReaderIndex = reader.Read7BitEncodedInt();
        if (typeReaderIndex <= 0)
            throw new InvalidDataException("Compiled effect XNB does not contain an Effect object.");
        var length = reader.ReadInt32();
        if (length <= 0 || length > stream.Length - stream.Position)
            throw new InvalidDataException("Compiled effect bytecode length is invalid.");
        return (platform, reader.ReadBytes(length));
    }

    private sealed class SingleEffectBuilder(string relativePath) : ContentBuilder
    {
        public override IContentCollection GetContentCollection()
        {
            var content = new ContentCollection();
            content.Include(relativePath);
            return content;
        }
    }
}
