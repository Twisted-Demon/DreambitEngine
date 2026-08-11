using System;
using System.IO;

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
        return Resources.OpenAssetStream(assetName, pakName, usePak, contentDirectory);
    }

    protected string GetPath(string assetName)
    {
        return assetName + Extension;
    }
}

public abstract class AssetLoaderBase<T> : AssetLoaderBase where T : class
{
    protected Logger<T> Logger = new();
}
