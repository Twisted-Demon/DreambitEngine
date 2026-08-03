using System;
using System.IO;
using Microsoft.Xna.Framework.Audio;

namespace Dreambit;

public sealed class SoundEffectLoader : AssetLoaderBase
{
    public override string Extension { get; } = ".audb";

    public override bool AddToDisposableList { get; } = true;

    public override Type TargetType { get; } =
        typeof(SoundEffect);

    public override object Load(
        string assetName,
        string pakName,
        bool usePak,
        string contentDirectory)
    {
        using var audbStream = GetStream(
            GetPath(assetName),
            pakName,
            usePak,
            contentDirectory);

        var (header, payload) =
            AudbLoader.ReadAudb(audbStream);

        try
        {
            var soundEffect = header.SubType switch
            {
                AudbLoader.AudioSubType.Wav =>
                    LoadWav(assetName, payload),

                AudbLoader.AudioSubType.Ogg or
                AudbLoader.AudioSubType.Mp3 =>
                    LoadCompressed(assetName, header, payload),

                _ => throw new NotSupportedException(
                    $"Unsupported AUDB subtype '{header.SubType}'.")
            };

            soundEffect.Name = assetName;
            return soundEffect;
        }
        catch (Exception exception)
        {
            throw new InvalidDataException(
                $"Failed to load audio asset '{assetName}' as a SoundEffect. " +
                $"Subtype: {header.SubType}. " +
                $"AUDB payload size: {payload.Length:N0} bytes. " +
                $"First bytes: {GetHexPreview(payload)}",
                exception);
        }
    }

    private static SoundEffect LoadWav(
        string assetName,
        byte[] payload)
    {
        ValidateWavPayload(assetName, payload);

        using var wavStream = new MemoryStream(
            payload,
            index: 0,
            count: payload.Length,
            writable: false,
            publiclyVisible: true);

        return SoundEffect.FromStream(wavStream);
    }

    private static SoundEffect LoadCompressed(
        string assetName,
        AudbLoader.AudbHeader header,
        byte[] payload)
    {
        var decoded = AudioDecoder.Decode(
            header.SubType,
            payload);

        ValidateBakedMetadata(
            assetName,
            header,
            decoded);

        return new SoundEffect(
            decoded.Data,
            decoded.SampleRate,
            decoded.Channels);
    }

    private static void ValidateBakedMetadata(
        string assetName,
        AudbLoader.AudbHeader header,
        AudioDecoder.DecodedPcm16 decoded)
    {
        var decodedChannelCount = decoded.Channels switch
        {
            AudioChannels.Mono => 1u,
            AudioChannels.Stereo => 2u,

            _ => throw new InvalidDataException(
                $"Decoded asset '{assetName}' has an unknown channel layout.")
        };

        // Metadata was zero in older AUDB files. Allow those files
        // to continue loading.
        if (header.Channels != 0 &&
            header.Channels != decodedChannelCount)
        {
            throw new InvalidDataException(
                $"Audio asset '{assetName}' channel metadata does not match " +
                $"the decoded stream. Header: {header.Channels}, " +
                $"decoded: {decodedChannelCount}.");
        }

        if (header.SampleRate != 0 &&
            header.SampleRate != decoded.SampleRate)
        {
            throw new InvalidDataException(
                $"Audio asset '{assetName}' sample-rate metadata does not " +
                $"match the decoded stream. Header: {header.SampleRate}, " +
                $"decoded: {decoded.SampleRate}.");
        }
    }

    private static void ValidateWavPayload(
        string assetName,
        ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 12)
        {
            throw new InvalidDataException(
                $"Audio asset '{assetName}' has an invalid WAV payload. " +
                $"Expected at least 12 bytes, received {payload.Length}.");
        }

        var hasRiff =
            payload[0] == (byte)'R' &&
            payload[1] == (byte)'I' &&
            payload[2] == (byte)'F' &&
            payload[3] == (byte)'F';

        var hasWave =
            payload[8] == (byte)'W' &&
            payload[9] == (byte)'A' &&
            payload[10] == (byte)'V' &&
            payload[11] == (byte)'E';

        if (!hasRiff || !hasWave)
        {
            throw new InvalidDataException(
                $"Audio asset '{assetName}' does not contain a complete " +
                $"RIFF/WAVE file after its AUDB header. " +
                $"First bytes: {GetHexPreview(payload)}");
        }
    }

    private static string GetHexPreview(
        ReadOnlySpan<byte> data)
    {
        var count = Math.Min(data.Length, 16);
        return Convert.ToHexString(data[..count]);
    }
}