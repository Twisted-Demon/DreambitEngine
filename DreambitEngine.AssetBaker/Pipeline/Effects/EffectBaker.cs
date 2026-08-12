using System.Buffers.Binary;
using DreambitEngine.AssetBaker.Abstractions;
using Microsoft.Xna.Framework.Content.Pipeline;
using MonoGame.Framework.Content.Pipeline.Builder;

namespace DreambitEngine.AssetBaker.Pipeline.Effects;

/// <summary>Compiles MonoGame .fx source and stores raw MGFX bytecode in an FXB envelope.</summary>
public sealed class EffectBaker : AssetBakerBase
{
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
            builder.Run([
                "build",
                "-p", NormalizePlatform(ctx.TargetPlatform),
                "-s", sourceRoot,
                "-o", outputRoot,
                "-i", intermediateRoot
            ]);
            if (builder.FailedToBuild > 0)
                throw new InvalidOperationException($"MonoGame failed to compile effect '{relativePath}'.");

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
                GetLogicalPath(ctx, OutputExtension),
                AssetType.Effect,
                OutputExtension,
                output.ToArray());
        }
        finally
        {
            try
            {
                Directory.Delete(temporaryRoot, true);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
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
