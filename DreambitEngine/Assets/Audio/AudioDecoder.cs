using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using Microsoft.Xna.Framework.Audio;
using NLayer;
using NVorbis;

namespace Dreambit;

internal static class AudioDecoder
{
    public static DecodedPcm16 Decode(
        AudbLoader.AudioSubType subType,
        byte[] payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        return subType switch
        {
            AudbLoader.AudioSubType.Ogg => DecodeOgg(payload),
            AudbLoader.AudioSubType.Mp3 => DecodeMp3(payload),

            _ => throw new NotSupportedException(
                $"Audio subtype '{subType}' cannot be decoded " +
                "as compressed audio.")
        };
    }

    private static DecodedPcm16 DecodeOgg(byte[] payload)
    {
        using var stream = CreateReadOnlyStream(payload);

        using var vorbisReader = new VorbisReader(
            stream,
            false);

        return DecodeFloatSamples(
            vorbisReader.SampleRate,
            vorbisReader.Channels,
            vorbisReader.ReadSamples);
    }

    private static DecodedPcm16 DecodeMp3(byte[] payload)
    {
        using var stream = CreateReadOnlyStream(payload);
        using var mpegFile = new MpegFile(stream);

        return DecodeFloatSamples(
            mpegFile.SampleRate,
            mpegFile.Channels,
            mpegFile.ReadSamples);
    }

    private static DecodedPcm16 DecodeFloatSamples(
        int sampleRate,
        int channelCount,
        Func<float[], int, int, int> readSamples)
    {
        var channels = ValidateSoundEffectFormat(
            sampleRate,
            channelCount);

        var sampleCapacity = 16 * 1024;

        // Both decoders return interleaved samples. Keep every read
        // aligned to a complete frame.
        sampleCapacity -= sampleCapacity % channelCount;

        var floatBuffer =
            ArrayPool<float>.Shared.Rent(sampleCapacity);

        var pcmBuffer =
            ArrayPool<byte>.Shared.Rent(
                checked(sampleCapacity * sizeof(short)));

        try
        {
            using var decodedStream = new MemoryStream();

            while (true)
            {
                var samplesRead = readSamples(
                    floatBuffer,
                    0,
                    sampleCapacity);

                if (samplesRead == 0)
                    break;

                if (samplesRead < 0)
                    throw new InvalidDataException(
                        "Audio decoder returned a negative sample count.");

                if (samplesRead % channelCount != 0)
                    throw new InvalidDataException(
                        "Audio decoder returned an incomplete sample frame.");

                var bytesToWrite =
                    checked(samplesRead * sizeof(short));

                var destination =
                    pcmBuffer.AsSpan(0, bytesToWrite);

                for (var i = 0; i < samplesRead; i++)
                {
                    var sample = floatBuffer[i];

                    if (!float.IsFinite(sample))
                        sample = 0f;

                    sample = Math.Clamp(sample, -1f, 1f);

                    var scaledSample = sample < 0f
                        ? (int)(sample * 32768f)
                        : (int)(sample * 32767f);

                    BinaryPrimitives.WriteInt16LittleEndian(
                        destination.Slice(
                            i * sizeof(short),
                            sizeof(short)),
                        (short)scaledSample);
                }

                decodedStream.Write(
                    pcmBuffer,
                    0,
                    bytesToWrite);
            }

            if (decodedStream.Length == 0)
                throw new InvalidDataException(
                    "The compressed audio stream decoded to zero samples.");

            return new DecodedPcm16(
                decodedStream.ToArray(),
                sampleRate,
                channels);
        }
        finally
        {
            ArrayPool<float>.Shared.Return(floatBuffer);
            ArrayPool<byte>.Shared.Return(pcmBuffer);
        }
    }

    private static AudioChannels ValidateSoundEffectFormat(
        int sampleRate,
        int channelCount)
    {
        // MonoGame SoundEffect accepts sample rates from
        // 8,000 Hz through 48,000 Hz.
        if (sampleRate is < 8000 or > 48000)
            throw new NotSupportedException(
                $"SoundEffect sample rate {sampleRate:N0} Hz is unsupported. " +
                "MonoGame requires a rate between 8,000 and 48,000 Hz.");

        return channelCount switch
        {
            1 => AudioChannels.Mono,
            2 => AudioChannels.Stereo,

            _ => throw new NotSupportedException(
                $"SoundEffect only supports mono or stereo audio. " +
                $"The asset contains {channelCount} channels.")
        };
    }

    private static MemoryStream CreateReadOnlyStream(byte[] data)
    {
        return new MemoryStream(
            data,
            0,
            data.Length,
            false,
            true);
    }

    internal readonly record struct DecodedPcm16(
        byte[] Data,
        int SampleRate,
        AudioChannels Channels);
}