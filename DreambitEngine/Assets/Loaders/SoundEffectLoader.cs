using System;
using System.IO;
using Microsoft.Xna.Framework.Audio;

namespace Dreambit;

public class SoundEffectLoader : AssetLoaderBase
{
    public override string Extension { get; } = ".audb";
    public override bool AddToDisposableList { get; } = true;
    public override Type TargetType { get; } = typeof(SoundEffect);

    public override object Load(string assetName, string pakName, bool usePak, string contentDirectory)
    {
        using var audbStream = GetStream(
            GetPath(assetName),
            pakName,
            usePak,
            contentDirectory);

        var (header, payload) = AudbLoader.ReadAudb(audbStream);

        if (header.SubType != AudbLoader.AudioSubType.Wav)
        {
            throw new NotSupportedException(
                $"SoundEffectLoader only supports WAV audio. " +
                $"Asset: '{assetName}', subtype: {header.SubType}.");
        }

        ValidateWavPayload(assetName, payload);

        using var wavStream = new MemoryStream(
            payload,
            index: 0,
            count: payload.Length,
            writable: false,
            publiclyVisible: true);

        wavStream.Position = 0;

        try
        {
            var soundEffect = SoundEffect.FromStream(wavStream);
            soundEffect.Name = assetName;
            return soundEffect;
        }
        catch (Exception exception)
        {
            throw new InvalidDataException(
                $"MonoGame failed to load WAV asset '{assetName}'. " +
                $"AUDB payload size: {payload.Length:N0} bytes. " +
                $"First bytes: {GetHexPreview(payload)}",
                exception);
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

        bool hasRiff =
            payload[0] == (byte)'R' &&
            payload[1] == (byte)'I' &&
            payload[2] == (byte)'F' &&
            payload[3] == (byte)'F';

        bool hasWave =
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

    private static string GetHexPreview(ReadOnlySpan<byte> data)
    {
        int count = Math.Min(data.Length, 16);
        return Convert.ToHexString(data[..count]);
    }
}