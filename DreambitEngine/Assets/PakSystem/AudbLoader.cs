using System;
using System.Buffers.Binary;
using System.IO;
using System.Security.Cryptography;
using Microsoft.Xna.Framework.Media;

namespace Dreambit;

public static class AudbLoader
{
    private const ushort CurrentVersion = 1;

    public enum AudioSubType : ushort
    {
        Wav = 0,
        Ogg = 1,
        Mp3 = 2
    }

    public static (
        AudbHeader Header,
        byte[] Payload) ReadAudb(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (!stream.CanRead)
        {
            throw new ArgumentException(
                "AUDB stream must be readable.",
                nameof(stream));
        }

        Span<byte> magic = stackalloc byte[4];
        stream.ReadExactly(magic);

        if (magic[0] != (byte)'A' ||
            magic[1] != (byte)'U' ||
            magic[2] != (byte)'D' ||
            magic[3] != (byte)'B')
        {
            throw new InvalidDataException(
                "Stream does not contain an AUDB header.");
        }

        var version = ReadUInt16(stream);

        if (version != CurrentVersion)
        {
            throw new NotSupportedException(
                $"Unsupported AUDB version {version}. " +
                $"Expected version {CurrentVersion}.");
        }

        var subType =
            (AudioSubType)ReadUInt16(stream);

        if (!Enum.IsDefined(typeof(AudioSubType), subType))
        {
            throw new InvalidDataException(
                $"AUDB contains unknown audio subtype {(ushort)subType}.");
        }

        var channels = ReadUInt16(stream);
        var sampleRate = ReadUInt32(stream);
        var flags = ReadUInt32(stream);
        var payloadSize = ReadUInt32(stream);

        if (payloadSize > int.MaxValue)
        {
            throw new InvalidDataException(
                $"AUDB payload is too large: {payloadSize:N0} bytes.");
        }

        if (stream.CanSeek)
        {
            var remainingBytes =
                stream.Length - stream.Position;

            if (remainingBytes < payloadSize)
            {
                throw new EndOfStreamException(
                    $"AUDB declares a {payloadSize:N0}-byte payload, " +
                    $"but only {remainingBytes:N0} bytes remain.");
            }
        }

        var payload =
            GC.AllocateUninitializedArray<byte>(
                checked((int)payloadSize));

        stream.ReadExactly(payload);

        var header = new AudbHeader(
            version,
            subType,
            channels,
            sampleRate,
            flags,
            payload.Length);

        return (header, payload);
    }

    // Preserves the old public API.
    public static Song LoadSong(
        Stream stream,
        string tempRoot = null)
    {
        return LoadSongAsset(
            stream,
            "song",
            tempRoot);
    }

    public static Song LoadSongAsset(
        Stream stream,
        string assetName,
        string tempRoot = null)
    {
        if (string.IsNullOrWhiteSpace(assetName))
        {
            throw new ArgumentException(
                "Song asset name cannot be empty.",
                nameof(assetName));
        }

        var (header, payload) = ReadAudb(stream);

        var extension = header.SubType switch
        {
            AudioSubType.Wav => ".wav",
            AudioSubType.Ogg => ".ogg",
            AudioSubType.Mp3 => ".mp3",

            _ => throw new NotSupportedException(
                $"Unsupported Song subtype '{header.SubType}'.")
        };

        var cacheRoot = string.IsNullOrWhiteSpace(tempRoot)
            ? Path.Combine(
                Path.GetTempPath(),
                "Dreambit",
                "AudioCache")
            : Path.GetFullPath(tempRoot);

        Directory.CreateDirectory(cacheRoot);

        // The file name depends on its contents. Loading the same song
        // repeatedly therefore reuses the same extracted file.
        var hash = SHA256.HashData(payload);

        var cachedFileName =
            Convert.ToHexString(hash).ToLowerInvariant() +
            extension;

        var cachedPath = Path.Combine(
            cacheRoot,
            cachedFileName);

        EnsureCachedFile(
            cachedPath,
            payload);

        return Song.FromUri(
            assetName,
            new Uri(Path.GetFullPath(cachedPath)));
    }

    private static void EnsureCachedFile(
        string destinationPath,
        byte[] payload)
    {
        if (File.Exists(destinationPath))
            return;

        var temporaryPath =
            destinationPath +
            "." +
            Guid.NewGuid().ToString("N") +
            ".tmp";

        try
        {
            File.WriteAllBytes(
                temporaryPath,
                payload);

            try
            {
                File.Move(
                    temporaryPath,
                    destinationPath);
            }
            catch (IOException) when (File.Exists(destinationPath))
            {
                // Another thread or process completed the same extraction.
            }
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }

    private static ushort ReadUInt16(Stream stream)
    {
        Span<byte> data = stackalloc byte[sizeof(ushort)];
        stream.ReadExactly(data);

        return BinaryPrimitives.ReadUInt16LittleEndian(data);
    }

    private static uint ReadUInt32(Stream stream)
    {
        Span<byte> data = stackalloc byte[sizeof(uint)];
        stream.ReadExactly(data);

        return BinaryPrimitives.ReadUInt32LittleEndian(data);
    }

    public sealed record AudbHeader(
        ushort Version,
        AudioSubType SubType,
        ushort Channels,
        uint SampleRate,
        uint Flags,
        int Size);
}