using System.Collections.Generic;

namespace Dreambit;

public class AssetQueue<T> where T : class
{
    internal Dictionary<string, T> Assets = [];
    internal Queue<string> AssetsToLoad = [];
    public bool FinishedLoading = false;

    public bool IsLoading = false;

    public int Count => AssetsToLoad.Count;

    public void Enqueue(string directory, params string[] assets)
    {
        foreach (var assetName in assets)
        {
            var assetNameWithDirectory = $"{directory}/{assetName}";

            if (AssetsToLoad.Contains(assetNameWithDirectory)) continue;
            AssetsToLoad.Enqueue(assetNameWithDirectory);
        }
    }

    internal void AddAsset(string assetName, T asset)
    {
        Assets.TryAdd(assetName, asset);
    }

    internal string GetNext()
    {
        return AssetsToLoad.Dequeue();
    }

    internal bool TryGetNext(out string assetName)
    {
        assetName = string.Empty;

        if (AssetsToLoad.Count == 0) return false;

        assetName = AssetsToLoad.Dequeue();
        return true;
    }
}