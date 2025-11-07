namespace Dreambit;

public class SongLoader : AssetLoaderBase
{
    public override string Extension { get; } = ".audb";
    public override bool AddToDisposableList { get; } = true;
    public override object Load(string assetName, string pakName, bool usePak, string contentDirectory)
    {
        using var s = GetStream(assetName, pakName, usePak, contentDirectory);
        return AudbLoader.LoadSong(s);
    }
}