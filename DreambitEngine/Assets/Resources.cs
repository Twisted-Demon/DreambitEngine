using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using FontStashSharp;
using Microsoft.Xna.Framework.Content;

namespace Dreambit;

public enum AssetContentMode
{
    /// <summary>Uses a PAK when present, otherwise a baked-blob manifest.</summary>
    Auto,
    Pak,
    Blobs,
    LooseFiles
}

public class Resources : Singleton<Resources>
{
    // Explicit loaders are discovered once per engine/game assembly generation. Generic loaders
    // are created lazily only for eligible game asset types and are released with that assembly.
    private static readonly Dictionary<Type, IAssetLoader> ExplicitLoaders = [];
    private static readonly Dictionary<Type, IAssetLoader> GenericDreambitLoaders = [];
    private readonly Dictionary<string, PakReader> _pakReaders =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, BlobContentReader> _blobReaders =
        new(StringComparer.OrdinalIgnoreCase);
    private DreambitXnbReader _xnbReader;
    private static string _contentDirectoryOverride;
    private static AssetContentMode _contentMode = AssetContentMode.Auto;

    private static string ContentDirectory =>
        string.IsNullOrWhiteSpace(_contentDirectoryOverride)
            ? Path.Combine(AppContext.BaseDirectory, Core.Instance.Content.RootDirectory)
            : _contentDirectoryOverride;
    /// <summary>
    /// Compatibility switch for callers that explicitly select PAK or loose-file loading.
    /// New development hosts should use <see cref="ContentMode"/>.
    /// </summary>
    public static bool UsePak
    {
        get => _contentMode is AssetContentMode.Auto or AssetContentMode.Pak;
        set => _contentMode = value ? AssetContentMode.Pak : AssetContentMode.LooseFiles;
    }

    public static AssetContentMode ContentMode
    {
        get => _contentMode;
        set => _contentMode = value;
    }
    public static string PakName { get; set; } = "content.pak";
    public static IAssetRegistry AssetRegistry { get; set; }

    public DreambitContentCollection ContentCollection { get; } = new();

    /// <summary>The directory currently used for PAK and loose content loading.</summary>
    public static string ActiveContentDirectory => ContentDirectory;

    /// <summary>
    /// Points resource loading at an externally built game's content directory.
    /// Existing cached content and PAK readers are released before the source changes.
    /// </summary>
    public static void SetContentSource(string contentDirectory, string pakName = "content.pak")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentDirectory);
        var fullPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(contentDirectory));
        if (!string.IsNullOrWhiteSpace(_contentDirectoryOverride) &&
            string.Equals(fullPath, _contentDirectoryOverride, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(pakName, PakName, StringComparison.OrdinalIgnoreCase))
            return;

        RefreshContent();
        _contentDirectoryOverride = fullPath;
        PakName = pakName;
    }

    /// <summary>Points resource loading at an incremental baked-blob directory.</summary>
    public static void SetBlobContentSource(string contentDirectory)
    {
        SetContentSource(contentDirectory);
        _contentMode = AssetContentMode.Blobs;
    }

    /// <summary>Restores the game's default Content directory and automatic source selection.</summary>
    public static void ResetContentSource()
    {
        RefreshContent();
        _contentDirectoryOverride = null;
        PakName = "content.pak";
        _contentMode = AssetContentMode.Auto;
    }

    /// <summary>Releases cached assets and PAK readers so newly baked content can be opened.</summary>
    public static void RefreshContent()
    {
        Instance.ReleaseEntries(Instance.ContentCollection.Drain());
        foreach (var reader in Instance._pakReaders.Values)
            reader.Dispose();
        Instance._pakReaders.Clear();
        Instance._blobReaders.Clear();
    }

    public void Init()
    {
        RefreshLoaders();
        _xnbReader ??= new DreambitXnbReader(
            Core.Instance.Services,
            Core.Instance.Content.RootDirectory);

        TryLoadRuntimeAssetRegistry();
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

            var loader = ResolveLoader(typeof(T));
            if (loader is not null)
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

            AssignDreambitAssetIdentity(typedAsset, assetName);

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

            var loader = ResolveLoader(type);
            if (loader is null)
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

            AssignDreambitAssetIdentity(asset, assetName);

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

    internal static void RefreshLoaders()
    {
        RefreshLoaders(null);
    }

    internal static void RefreshLoaders(IEnumerable<Type>? additionalLoaderTypes)
    {
        ExplicitLoaders.Clear();
        GenericDreambitLoaders.Clear();
        var loaderTypes = ReflectionUtils.GetAllTypesAssignableFrom(typeof(IAssetLoader), true)
            .Where(type => AssemblyLoadContext.GetLoadContext(type.Assembly)?.IsCollectible != true);
        if (additionalLoaderTypes is not null)
        {
            loaderTypes = loaderTypes.Concat(additionalLoaderTypes.Where(type =>
                typeof(IAssetLoader).IsAssignableFrom(type) &&
                !type.IsAbstract &&
                !type.IsGenericType &&
                type.GetConstructor(Type.EmptyTypes) is not null));
        }

        foreach (var type in loaderTypes)
        {
            var instance = (IAssetLoader)Activator.CreateInstance(type);
            if (instance is not null)
                ExplicitLoaders[instance.TargetType] = instance;
        }
    }

    private void TryLoadRuntimeAssetRegistry()
    {
        try
        {
            using var stream = OpenAssetStream(
                RuntimeAssetRegistry.LogicalPath,
                PakName,
                UsePak,
                ContentDirectory);
            AssetRegistry = RuntimeAssetRegistry.Load(stream);
            Logger.Trace("Loaded runtime asset registry.");
        }
        catch (FileNotFoundException)
        {
            // Legacy content does not contain a stable-ID manifest.
        }
        catch (DirectoryNotFoundException)
        {
            // Content may not have been built yet in a development checkout.
        }
        catch (Exception exception)
        {
            Logger.Warn("Could not load the runtime asset registry: {0}", exception.Message);
        }
    }

    public static object LoadDreambitAsset(
        AssetId assetId,
        string fallbackAssetName,
        Type type)
    {
        if (assetId.IsEmpty)
            return null;

        var assetName = fallbackAssetName;
        if (AssetRegistry?.TryResolveAssetName(assetId, out var resolvedAssetName) == true)
            assetName = resolvedAssetName;

        if (string.IsNullOrWhiteSpace(assetName))
            return null;

        var asset = LoadDreambitAsset(assetName, type);
        if (asset is DreambitAsset dreambitAsset)
            dreambitAsset.AssetId = assetId;

        return asset;
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
            if (ResolveLoader(typeof(SpriteFontBase)) is { } loader)
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
        var mode = _contentMode;
        if (mode == AssetContentMode.Blobs)
            return OpenBlobStream(assetName, contentDirectory);

        // The caller's explicit source selection is authoritative for PAK versus
        // loose files. ContentMode.LooseFiles is already reflected by UsePak when
        // normal loaders call this method; honoring it here as a second override
        // made direct loaders read an unrelated global source during editor work.
        if (!usePak)
            return File.OpenRead(Path.Combine(contentDirectory, assetName));

        var pakPath = Path.GetFullPath(Path.Combine(contentDirectory, pakName));
        if (mode == AssetContentMode.Auto && !File.Exists(pakPath))
        {
            var manifestPath = Path.Combine(contentDirectory, BlobContentManifest.FileName);
            if (File.Exists(manifestPath))
                return OpenBlobStream(assetName, contentDirectory);
        }

        if (!Instance._pakReaders.TryGetValue(pakPath, out var reader))
        {
            reader = new PakReader(pakPath);
            Instance._pakReaders.Add(pakPath, reader);
        }

        return reader.Open(assetName);
    }

    private static Stream OpenBlobStream(string assetName, string contentDirectory)
    {
        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(contentDirectory));
        if (!Instance._blobReaders.TryGetValue(root, out var reader))
        {
            reader = new BlobContentReader(root);
            Instance._blobReaders.Add(root, reader);
        }

        return reader.Open(assetName);
    }

    internal void CleanUp()
    {
        ResetContentSource();
        _xnbReader?.Dispose();
        _xnbReader = null;
    }

    internal static void ReleaseAssembly(Assembly assembly)
    {
        foreach (var type in ExplicitLoaders
                     .Where(pair =>
                         pair.Key.Assembly == assembly ||
                         pair.Value.GetType().Assembly == assembly)
                     .Select(pair => pair.Key)
                     .ToArray())
        {
            ExplicitLoaders.Remove(type);
        }

        foreach (var type in GenericDreambitLoaders.Keys
                     .Where(type => type.Assembly == assembly)
                     .ToArray())
        {
            GenericDreambitLoaders.Remove(type);
        }

        Instance.ReleaseEntries(Instance.ContentCollection.RemoveAssembly(assembly));
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

    private static IAssetLoader? ResolveLoader(Type type)
    {
        if (ExplicitLoaders.TryGetValue(type, out var explicitLoader))
            return explicitLoader;

        if (!typeof(DreambitAsset).IsAssignableFrom(type) ||
            !DreambitAssetTypeRegistry.CanUseGenericJsonLoader(type))
        {
            return null;
        }

        if (!GenericDreambitLoaders.TryGetValue(type, out var genericLoader))
        {
            genericLoader = new GenericDreambitAssetLoader(type);
            GenericDreambitLoaders.Add(type, genericLoader);
        }

        return genericLoader;
    }

    private static void AssignDreambitAssetIdentity(object asset, string requestedAssetName)
    {
        if (asset is not DreambitAsset dreambitAsset)
            return;

        if (string.IsNullOrWhiteSpace(dreambitAsset.AssetName))
            dreambitAsset.AssetName = requestedAssetName;

        if (AssetRegistry?.TryGetAssetId(dreambitAsset.AssetName, out var assetId) == true)
            dreambitAsset.AssetId = assetId;
    }
}
