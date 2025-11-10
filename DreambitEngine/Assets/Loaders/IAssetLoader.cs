using System;
using System.IO;
using System.Text;

namespace Dreambit;

public interface IAssetLoader
{
    string Extension { get; }
    bool AddToDisposableList { get; }
    Type TargetType { get; }
    object Load(string assetName, string pakName, bool usePak, string contentDirectory);
}

public abstract class AssetLoaderBase : IAssetLoader
{
    public abstract string Extension { get; }
    public abstract bool AddToDisposableList { get; }
    public abstract Type TargetType { get; }
    public abstract object Load(string assetName, string pakName, bool usePak, string contentDirectory);
    
    protected static Stream GetStream(string assetName, string pakName, bool usePak, string contentDirectory)
    {
        if (usePak)
        {
            var pak = new PakReader(Path.Combine(contentDirectory, pakName));
            return pak.Open(assetName);
        }
        else
        {
            return File.OpenRead(Path.Combine(contentDirectory, assetName));
        }
    }

    protected string GetPath(string assetName)
    {
        return assetName + Extension;
    }
}

public abstract class AssetLoaderBase<T> : AssetLoaderBase where T: class
{
    protected Logger<T> Logger = new Logger<T>();
}