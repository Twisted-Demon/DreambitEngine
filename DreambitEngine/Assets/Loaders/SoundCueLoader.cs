namespace Dreambit;

public class SoundCueLoader : AssetLoaderBase
{
    public override string Extension { get; } = ".jsonb";
    public override bool AddToDisposableList { get; } = true;
    public override object Load(string assetName, string pakName, bool usePak, string contentDirectory)
    {
        using var s = GetStream(GetPath(assetName), pakName, usePak, contentDirectory);
        
        var cue = JsnbLoader.Deserialize<SoundCue>(s);
        cue.LoadInternal();
        cue.AssetName = assetName;
        
        return cue;
    }
}