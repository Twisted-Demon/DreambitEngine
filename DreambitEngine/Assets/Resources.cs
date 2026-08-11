using System;
using System.Collections.Generic;
using System.IO;
using FontStashSharp;
using Microsoft.Xna.Framework.Content;

namespace Dreambit;

public class Resources : Singleton<Resources>
{
    private static readonly Dictionary<Type, IAssetLoader> Loaders = [];
    private readonly Dictionary<string, PakReader> _pakReaders =
        new(StringComparer.OrdinalIgnoreCase);
    private DreambitXnbReader _xnbReader;

    private static string ContentDirectory =>
        Path.Combine(AppContext.BaseDirectory, Core.Instance.Content.RootDirectory);
    public static bool UsePak { get; set; } = true;
    public static string PakName { get; set; } = "content.pak";

    public DreambitContentCollection ContentCollection { get; } = new();

    public void Init()
    {
        Loaders.Clear();
        _xnbReader ??= new DreambitXnbReader(
            Core.Instance.Services,
            Core.Instance.Content.RootDirectory);

        var loaderTypes = ReflectionUtils.GetAllTypesAssignableFrom(
            typeof(IAssetLoader),
            true);

        foreach (var type in loaderTypes)
        {
            var instance = (IAssetLoader)Activator.CreateInstance(type);
            if (instance is null) continue;

            Loaders[instance.TargetType] = instance;
        }
    }

    /// <summary>
    ///     Tries to Load an asset and returns default if not found
    /// </summary>
    /// <param name="assetName"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T LoadAsset<T>(string assetName) where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assetName);

        if (Instance.ContentCollection.TryGet<T>(assetName, out var cachedAsset))
            return cachedAsset;

        try
        {
            Instance.Logger.Trace("Loading {0} - {1}", typeof(T).Name, assetName);

            object asset;
            var ownsAsset = false;
            IReadOnlyList<IDisposable> ownedDisposables = null;

            if (Loaders.TryGetValue(typeof(T), out var loader))
            {
                asset = loader.Load(assetName, PakName, UsePak, ContentDirectory);
                ownsAsset = loader.AddToDisposableList;
            }
            else
            {
                var disposables = new List<IDisposable>();
                asset = Instance._xnbReader.Read<T>(assetName, disposables.Add);
                ownedDisposables = disposables;
            }

            if (asset is not T typedAsset)
                throw new ContentLoadException(
                    $"The loader for '{assetName}' did not return {typeof(T).FullName}.");

            Instance.ContentCollection.TryAdd(
                assetName,
                typeof(T),
                typedAsset,
                ownsAsset,
                ownedDisposables);

            return typedAsset;
        }
        catch (Exception exception)
        {
            var logger = Instance.Logger;

            logger.Error(
                "Asset loading failed.\n" +
                $"Asset path: {assetName}\n" +
                $"Asset type: {nameof(T)}\n" +
                $"Exception: {exception}");

            return null;
        }
    }

    public static object LoadDreambitAsset(string assetName, Type type)
    {
        if (!type.IsSubclassOf(typeof(DreambitAsset)))
            return null;

        if (Instance.ContentCollection.TryGet(assetName, type, out var cachedAsset))
            return cachedAsset;

        try
        {
            Instance.Logger.Trace("Loading {0} - {1}", type.Name, assetName);

            if (!Loaders.TryGetValue(type, out var loader))
                throw new ContentLoadException($"No Dreambit loader is registered for {type.FullName}.");

            var asset = loader.Load(assetName, PakName, UsePak, ContentDirectory);
            if (asset is null || !type.IsInstanceOfType(asset))
                throw new ContentLoadException(
                    $"The loader for '{assetName}' did not return {type.FullName}.");

            Instance.ContentCollection.TryAdd(
                assetName,
                type,
                asset,
                loader.AddToDisposableList);

            return asset;
        }
        catch (Exception e)
        {
            Instance.Logger.Warn("Could not load {0} | {1}", type.Name, assetName);
            Instance.Logger.Error(e.Message);

            return null;
        }
    }

    public static void UnloadAsset(string assetName)
    {
        var entries = Instance.ContentCollection.Remove(assetName);
        Instance.ReleaseEntries(entries);
    }

    public static bool TryRegisterAsset(DreambitAsset asset)
    {
        ArgumentNullException.ThrowIfNull(asset);

        return Instance.ContentCollection.TryAdd(
            asset.AssetName,
            asset.GetType(),
            asset,
            false);
    }

    public static SpriteFontBase LoadSpriteFont(string assetName, float fontSize = 12f)
    {
        var cacheName = GetFontCacheName(assetName, fontSize);
        if (Instance.ContentCollection.TryGet<SpriteFontBase>(cacheName, out var cachedFont))
            return cachedFont;

        try
        {
            Instance.Logger.Trace("Loading SpriteFontBase - {0}", cacheName);

            SpriteFontBase font;
            if (Loaders.TryGetValue(typeof(SpriteFontBase), out var loader))
            {
                var sfLoader = (SpriteFontBaseLoader)loader;

                font = sfLoader.LoadFont(assetName, ContentDirectory, fontSize);
            }
            else
            {
                Instance.Logger.Warn("Could not load {0} | {1}", nameof(SpriteFontBase), assetName + fontSize);
                return null;
            }

            Instance.ContentCollection.TryAdd(
                cacheName,
                typeof(SpriteFontBase),
                font,
                true);

            return font;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }


    internal static Stream OpenAssetStream(
        string assetName,
        string pakName,
        bool usePak,
        string contentDirectory)
    {
        if (!usePak)
            return File.OpenRead(Path.Combine(contentDirectory, assetName));

        var pakPath = Path.GetFullPath(Path.Combine(contentDirectory, pakName));
        if (!Instance._pakReaders.TryGetValue(pakPath, out var reader))
        {
            reader = new PakReader(pakPath);
            Instance._pakReaders.Add(pakPath, reader);
        }

        return reader.Open(assetName);
    }

    internal void CleanUp()
    {
        ReleaseEntries(ContentCollection.Drain());
        _xnbReader?.Dispose();
        _xnbReader = null;

        foreach (var reader in _pakReaders.Values)
            reader.Dispose();

        _pakReaders.Clear();
    }

    private static string GetFontCacheName(string assetName, float fontSize)
    {
        return $"{assetName}#font-size={fontSize:R}";
    }

    private void ReleaseEntries(IReadOnlyList<DreambitContentCollection.Entry> entries)
    {
        var disposed = new HashSet<object>(ReferenceEqualityComparer.Instance);

        foreach (var entry in entries)
        {
            if (entry.OwnedDisposables != null)
                foreach (var ownedDisposable in entry.OwnedDisposables)
                    if (ownedDisposable != null && disposed.Add(ownedDisposable))
                        ownedDisposable.Dispose();

            if (entry.OwnsAsset &&
                entry.Asset is IDisposable disposable &&
                disposed.Add(entry.Asset))
                disposable.Dispose();
        }
    }
}
