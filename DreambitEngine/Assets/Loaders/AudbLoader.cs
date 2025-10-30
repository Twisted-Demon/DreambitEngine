using System;
using System.Buffers.Binary;
using System.IO;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;

namespace Dreambit;

public static class AudbLoader
{
    public enum AudioSubType : ushort
    {
        Wav = 0,
        Ogg = 1,
        Mp3 = 2
    }

    public sealed record AudbHeader(
        ushort Version,
        AudioSubType SubType,
        ushort Channels,
        uint SampleRate,
        uint Flags,
        int Size);

    public static (AudbHeader hdr, byte[] payload) ReadAudb(Stream s)
    {
        Span<byte> h = stackalloc byte[16];
        
        s.ReadExactly(h[..4]);
        if (h[0] != (byte)'A' || h[1] != (byte)'U' || h[2] != (byte)'D' || h[3] != (byte)'B')
            throw new InvalidDataException("Not AUDB");
        
        s.ReadExactly(h[..2]);
        var ver = BinaryPrimitives.ReadUInt16LittleEndian(h[..2]);
        
        if(ver != 1) throw new NotSupportedException("Wrong AUDB version");
        
        s.ReadExactly(h[..2]); var sub = (AudioSubType)BinaryPrimitives.ReadUInt16LittleEndian(h[..2]);
        s.ReadExactly(h[..2]); var ch  = BinaryPrimitives.ReadUInt16LittleEndian(h[..2]);
        s.ReadExactly(h[..4]); var sr  = BinaryPrimitives.ReadUInt32LittleEndian(h[..4]);
        s.ReadExactly(h[..4]); var flg = BinaryPrimitives.ReadUInt32LittleEndian(h[..4]);
        s.ReadExactly(h[..4]); var sz  = (int)BinaryPrimitives.ReadUInt32LittleEndian(h[..4]);

        byte[] data = GC.AllocateUninitializedArray<byte>(sz);
        s.ReadExactly(data);

        return (new AudbHeader(ver, sub, ch, sr, flg, sz), data);
    }

    public static SoundEffect LoadSoundEffect(Stream s)
    {
        var (hdr, payload) = ReadAudb(s);
        if (hdr.SubType != AudioSubType.Wav)
            throw new NotSupportedException("Sound effect loader supports WAV only");

        using var ms = new MemoryStream(payload, writable: false);
        return SoundEffect.FromStream(ms);
    }
    
    public static Song LoadSong(Stream s, string tempRoot = null)
    {
        var (hdr, payload) = ReadAudb(s);

        var ext = hdr.SubType switch
        {
            AudioSubType.Wav => ".wav",
            AudioSubType.Ogg => ".ogg",
            AudioSubType.Mp3 => ".mp3",
            _ => ".dat"
        };

        var root = tempRoot ?? Path.Combine(Path.GetTempPath(), "GameAudioCache");
        Directory.CreateDirectory(root);
        // Unique temp file 
        var file = Path.Combine(root, Guid.NewGuid().ToString("N") + ext);
        File.WriteAllBytes(file, payload);

        // Song.FromUri works with file:// URIs
        var uri = new Uri(file);
        return Song.FromUri(Path.GetFileNameWithoutExtension(file), uri);
    }
}