using System;
using System.Collections.Generic;

namespace Dreambit;

/// <summary>
///     Dreambit-owned asset registry. This is the authoritative cache for engine
///     assets; MonoGame's ContentManager is only used as a loader for asset types
///     that require its compiled-content readers.
/// </summary>
public sealed class DreambitContentCollection
{
    private readonly Dictionary<AssetKey, Entry> _assets = [];

    public int Count => _assets.Count;

    public bool TryGet<T>(string assetName, out T asset) where T : class
    {
        if (_assets.TryGetValue(new AssetKey(assetName, typeof(T)), out var entry) &&
            entry.Asset is T typedAsset)
        {
            asset = typedAsset;
            return true;
        }

        asset = null;
        return false;
    }

    internal bool TryGet(string assetName, Type type, out object asset)
    {
        if (_assets.TryGetValue(new AssetKey(assetName, type), out var entry))
        {
            asset = entry.Asset;
            return true;
        }

        asset = null;
        return false;
    }

    internal bool TryAdd(
        string assetName,
        Type type,
        object asset,
        bool ownsAsset,
        IReadOnlyList<IDisposable> ownedDisposables = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assetName);
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(asset);

        return _assets.TryAdd(
            new AssetKey(assetName, type),
            new Entry(assetName, type, asset, ownsAsset, ownedDisposables));
    }

    internal List<Entry> Remove(string assetName)
    {
        var removed = new List<Entry>();
        if (string.IsNullOrWhiteSpace(assetName))
            return removed;

        foreach (var pair in _assets)
            if (StringComparer.OrdinalIgnoreCase.Equals(pair.Key.AssetName, Normalize(assetName)))
                removed.Add(pair.Value);

        foreach (var entry in removed)
            _assets.Remove(new AssetKey(entry.AssetName, entry.Type));

        return removed;
    }

    internal List<Entry> Drain()
    {
        var entries = new List<Entry>(_assets.Values);
        _assets.Clear();
        return entries;
    }

    private static string Normalize(string assetName)
    {
        return assetName.Replace('\\', '/').Trim();
    }

    private readonly struct AssetKey : IEquatable<AssetKey>
    {
        public AssetKey(string assetName, Type type)
        {
            AssetName = Normalize(assetName);
            Type = type;
        }

        public string AssetName { get; }
        private Type Type { get; }

        public bool Equals(AssetKey other)
        {
            return Type == other.Type &&
                   StringComparer.OrdinalIgnoreCase.Equals(AssetName, other.AssetName);
        }

        public override bool Equals(object obj)
        {
            return obj is AssetKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                StringComparer.OrdinalIgnoreCase.GetHashCode(AssetName),
                Type);
        }
    }

    internal readonly record struct Entry(
        string AssetName,
        Type Type,
        object Asset,
        bool OwnsAsset,
        IReadOnlyList<IDisposable> OwnedDisposables);
}
