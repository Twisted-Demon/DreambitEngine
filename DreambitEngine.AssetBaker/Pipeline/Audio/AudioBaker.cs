using System;
using System.Buffers.Binary;
using System.IO;
using DreambitEngine.AssetBaker.Abstractions;
using DreambitEngine.AssetBaker.Pipeline;
using NLayer;
using NVorbis;

namespace DreambitEngine.AssetBaker.pipeline.Audio;

public sealed class AudioBaker : AssetBakerBase
{
    private enum AudioSubType : ushort
    {
        Wav = 0,
        Ogg = 1,
        Mp3 = 2
    }

    private readonly record struct AudioInfo(
        AudioSubType SubType,
        ushort Channels,
        uint SampleRate);

    public override string AssetTypeName { get; } = "audio";

    public override string[] SupportedInputs { get; } =
    [
        ".wav",
        ".ogg",
        ".mp3"
    ];

    public override string OutputExtension => ".audb";

    public override void Bake(BakeContext ctx)
    {
        var blob = BakeToBytes(ctx);

        var outputDirectory = Path.GetDirectoryName(
            Path.GetFullPath(ctx.OutputPath));

        if (outputDirectory is not null)
            Directory.CreateDirectory(outputDirectory);

        File.WriteAllBytes(ctx.OutputPath, blob.Data);
    }

    public override AssetBlob BakeToBytes(BakeContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);

        if (!File.Exists(ctx.InputPath))
        {
            throw new FileNotFoundException(
                "The source audio file does not exist.",
                ctx.InputPath);
        }

        var extension = Path
            .GetExtension(ctx.InputPath)
            .ToLowerInvariant();

        var sourceBytes = File.ReadAllBytes(ctx.InputPath);

        var audioInfo = extension switch
        {
            ".wav" => ReadWavInfo(sourceBytes),
            ".ogg" => ReadOggInfo(sourceBytes),
            ".mp3" => ReadMp3Info(sourceBytes),

            _ => throw new NotSupportedException(
                $"Unsupported audio format '{extension}'.")
        };

        using var output = new MemoryStream(
            checked(sourceBytes.Length + 32));

        AudbWriter.Write(
            output,
            sourceBytes,
            (ushort)audioInfo.SubType,
            audioInfo.Channels,
            audioInfo.SampleRate);

        var logicalPath = GetLogicalPath(
            ctx,
            OutputExtension);

        return new AssetBlob(
            logicalPath,
            AssetType.Audio,
            OutputExtension,
            output.ToArray());
    }

    private AudioInfo ReadWavInfo(ReadOnlySpan<byte> data)
    {
        ParseWavHeader(
            data,
            out var channels,
            out var sampleRate);

        return new AudioInfo(
            AudioSubType.Wav,
            channels,
            sampleRate);
    }

    private static AudioInfo ReadOggInfo(byte[] data)
    {
        try
        {
            using var stream = new MemoryStream(
                data,
                index: 0,
                count: data.Length,
                writable: false,
                publiclyVisible: true);

            using var vorbisReader = new VorbisReader(
                stream,
                closeOnDispose: false);

            return CreateAudioInfo(
                AudioSubType.Ogg,
                vorbisReader.Channels,
                vorbisReader.SampleRate);
        }
        catch (Exception exception)
        {
            throw new InvalidDataException(
                "The OGG asset is not a valid Ogg Vorbis stream.",
                exception);
        }
    }

    private static AudioInfo ReadMp3Info(byte[] data)
    {
        try
        {
            using var stream = new MemoryStream(
                data,
                index: 0,
                count: data.Length,
                writable: false,
                publiclyVisible: true);

            using var mpegFile = new MpegFile(stream);

            return CreateAudioInfo(
                AudioSubType.Mp3,
                mpegFile.Channels,
                mpegFile.SampleRate);
        }
        catch (Exception exception)
        {
            throw new InvalidDataException(
                "The MP3 asset does not contain a valid MPEG audio stream.",
                exception);
        }
    }

    private static AudioInfo CreateAudioInfo(
        AudioSubType subType,
        int channels,
        int sampleRate)
    {
        if (channels <= 0 || channels > ushort.MaxValue)
        {
            throw new InvalidDataException(
                $"Invalid audio channel count: {channels}.");
        }

        if (sampleRate <= 0)
        {
            throw new InvalidDataException(
                $"Invalid audio sample rate: {sampleRate}.");
        }

        return new AudioInfo(
            subType,
            checked((ushort)channels),
            checked((uint)sampleRate));
    }

    public void ParseWavHeader(
        ReadOnlySpan<byte> data,
        out ushort channels,
        out uint sampleRate)
    {
        channels = 0;
        sampleRate = 0;

        if (data.Length < 12)
        {
            throw new InvalidDataException(
                "WAV file is too small to contain a RIFF/WAVE header.");
        }

        var hasRiffHeader =
            data[0] == (byte)'R' &&
            data[1] == (byte)'I' &&
            data[2] == (byte)'F' &&
            data[3] == (byte)'F';

        var hasWaveHeader =
            data[8] == (byte)'W' &&
            data[9] == (byte)'A' &&
            data[10] == (byte)'V' &&
            data[11] == (byte)'E';

        if (!hasRiffHeader)
            throw new InvalidDataException("Audio file is not a RIFF file.");

        if (!hasWaveHeader)
            throw new InvalidDataException("RIFF file is not a WAVE file.");

        var declaredRiffSize =
            BinaryPrimitives.ReadUInt32LittleEndian(data[4..8]);

        if (declaredRiffSize == uint.MaxValue)
        {
            throw new InvalidDataException(
                "The WAV uses an invalid or unsupported RIFF size of " +
                "0xFFFFFFFF. Re-export it as a standard RIFF/WAVE file.");
        }

        var foundFormatChunk = false;
        var foundDataChunk = false;
        var offset = 12;

        while (offset + 8 <= data.Length)
        {
            var chunkId =
                BinaryPrimitives.ReadUInt32LittleEndian(
                    data.Slice(offset, 4));

            var chunkSize =
                BinaryPrimitives.ReadUInt32LittleEndian(
                    data.Slice(offset + 4, 4));

            offset += 8;

            var chunkEnd = (long)offset + chunkSize;

            if (chunkEnd > data.Length)
            {
                throw new InvalidDataException(
                    "WAV contains a chunk extending beyond the end of the file.");
            }

            // "fmt "
            if (chunkId == 0x20746D66u)
            {
                if (chunkSize < 16)
                {
                    throw new InvalidDataException(
                        "WAV fmt chunk is smaller than 16 bytes.");
                }

                channels =
                    BinaryPrimitives.ReadUInt16LittleEndian(
                        data.Slice(offset + 2, 2));

                sampleRate =
                    BinaryPrimitives.ReadUInt32LittleEndian(
                        data.Slice(offset + 4, 4));

                foundFormatChunk = true;
            }
            // "data"
            else if (chunkId == 0x61746164u)
            {
                foundDataChunk = true;
            }

            var nextOffset =
                chunkEnd + (chunkSize & 1u);

            if (nextOffset > data.Length)
            {
                throw new InvalidDataException(
                    "WAV chunk padding extends beyond the end of the file.");
            }

            offset = checked((int)nextOffset);

            if (foundFormatChunk && foundDataChunk)
                break;
        }

        if (!foundFormatChunk)
        {
            throw new InvalidDataException(
                "WAV does not contain a fmt chunk.");
        }

        if (!foundDataChunk)
        {
            throw new InvalidDataException(
                "WAV does not contain a data chunk.");
        }

        if (channels == 0)
        {
            throw new InvalidDataException(
                "WAV reports zero audio channels.");
        }

        if (sampleRate == 0)
        {
            throw new InvalidDataException(
                "WAV reports a sample rate of zero.");
        }
    }
}