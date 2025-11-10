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
        using var s = GetStream(GetPath(assetName), pakName, usePak, contentDirectory);

        var (hdr, payload)  = AudbLoader.ReadAudb(s);
        if (hdr.SubType != AudbLoader.AudioSubType.Wav)
            throw new NotSupportedException("Sound effect loader supports WAV only");
        
        using var ms = new MemoryStream(payload, writable: false);
        var sfx = SoundEffect.FromStream(ms);
        sfx.Name = assetName;
        return sfx;
    }
}