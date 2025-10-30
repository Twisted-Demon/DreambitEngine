using System.Buffers.Binary;
using System.Security.Cryptography;
using DreambitEngine.AssetBaker.Abstractions;
using DreambitEngine.AssetBaker.Pipeline;
using SixLabors.ImageSharp.Formats.Qoi;

namespace DreambitEngine.AssetBaker.pipeline.Audio;

public class AudioBaker : AssetBakerBase
{
    public override string AssetTypeName { get; } = "audio";
    public override string[] SupportedInputs { get; } = [".wav", ".ogg", ".mp3"];
    public override string OutputExtension => ".audb";
    public override void Bake(BakeContext ctx)
    {
        var blob = BakeToBytes(ctx);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(ctx.OutputPath))!);
        File.WriteAllBytes(ctx.OutputPath, blob.Data);
    }

    public override AssetBlob BakeToBytes(BakeContext ctx)
    {
        var ext = Path.GetExtension(ctx.InputPath).ToLowerInvariant();
        var bytes = File.ReadAllBytes(ctx.InputPath);

        ushort subType;
        ushort channels = 0;
        uint sampleRate = 0;

        switch (ext)
        {
            case ".wav":
                subType = 0;
                break;
            case ".ogg":
                subType = 1;
                if (bytes.Length < 4 || bytes[0] != (byte)'O' || bytes[1] != (byte)'g' || bytes[2] != (byte)'g' || bytes[3] != (byte)'S')
                    throw new InvalidDataException("Not an Ogg stream.");
                break;
            case ".mp3":
                subType = 2;
                break;
            default:
                throw new NotSupportedException($"Not supported audio format: {ext}");
        }

        using var ms = new MemoryStream(bytes.Length + 32);
        AudbWriter.Write(ms, bytes, subType, channels, sampleRate);
        var data = ms.ToArray();

        var logical = GetLogicalPath(ctx, OutputExtension);
        return new AssetBlob(logical, AssetType.Audio, OutputExtension, data);
    }

    public void ParseWavHeader(ReadOnlySpan<byte> data, out ushort channels, out uint sampleRate)
    {
        channels = 0;
        sampleRate = 0;

        if (data.Length < 44) throw new InvalidOperationException("WAV too small");
        //RIFF
        if (data[0] != (byte)'R' || data[1] != (byte)'I' || data[2] != (byte)'F' || data[3] != (byte)'F')
            throw new InvalidDataException("Not RIFF.");
        //WAVE
        if (data[8] != (byte)'W' || data[9] != (byte)'A' || data[10] != (byte)'V' || data[11] != (byte)'E')
            throw new InvalidDataException("Not WAVE.");

        int offset = 12;
        
        while (offset + 8 <= data.Length)
        {
            uint chunkId = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..(offset+4)]);
            int  chunkSz = (int)BinaryPrimitives.ReadUInt32LittleEndian(data[(offset+4)..(offset+8)]);
            offset += 8;
            if (offset + chunkSz > data.Length) break;

            // "fmt "
            if (chunkId == 0x20746D66u) // 'f''m''t'' '
            {
                if (chunkSz < 16) throw new InvalidDataException("fmt chunk too small.");
                ushort audioFormat = BinaryPrimitives.ReadUInt16LittleEndian(data[offset..(offset+2)]); // 1=PCM, 2=ADPCM, 3=IEEE float, 17=IMA ADPCM
                channels  = BinaryPrimitives.ReadUInt16LittleEndian(data[(offset+2)..(offset+4)]);
                sampleRate= BinaryPrimitives.ReadUInt32LittleEndian(data[(offset+4)..(offset+8)]);
                // we accept PCM/ADPCM/Float; MonoGame SoundEffect.FromStream supports PCM/ADPCM; float often works if WAV header is correct
                break;
            }
            offset += chunkSz + (chunkSz & 1); // chunks are word-aligned
        }

        if (channels == 0 || sampleRate == 0)
            throw new InvalidDataException("WAV missing format info.");
    }
}