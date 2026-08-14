using System;

namespace Dreambit.Tiled;

/// <summary>
/// Base scene backed by one Tiled TMX map. The map is materialized into transient
/// Dreambit entities during initialization and unloaded with the scene.
/// </summary>
public class TiledScene : Scene
{
    private readonly string _mapAssetName;
    private readonly TiledMapImporter _importer = new();

    protected TiledScene(string mapAssetName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mapAssetName);
        _mapAssetName = mapAssetName;
    }

    public TmxMap Map { get; private set; }
    public TiledMapInstance MapInstance { get; private set; }
    public bool IsMapLoaded => MapInstance is { IsUnloaded: false };

    protected sealed override void OnInitialize()
    {
        var manager = TiledManager.Instance;
        manager.Initialize(_mapAssetName);
        Map = manager.Map;
        OnBeforeTiledMapLoaded();
        LoadMap();
        OnTiledSceneReady();
    }

    protected sealed override void OnEnd()
    {
        OnTiledSceneEnding();
        UnloadMap();
    }

    public TiledMapInstance LoadMap()
    {
        if (Map is null)
            throw new InvalidOperationException("The Tiled scene has not initialized its map yet.");
        if (IsMapLoaded)
            return MapInstance;

        var instance = _importer.Import(this, Map, CreateTiledImportOptions());
        MapInstance = instance;
        try
        {
            OnTiledMapLoaded(instance);
            return instance;
        }
        catch
        {
            MapInstance = null;
            instance.Unload();
            throw;
        }
    }

    public bool UnloadMap()
    {
        if (!IsMapLoaded)
            return false;
        var instance = MapInstance;
        OnTiledMapUnloading(instance);
        instance.Unload();
        MapInstance = null;
        OnTiledMapUnloaded(Map);
        return true;
    }

    protected virtual TiledImportOptions CreateTiledImportOptions() => new();

    protected virtual void OnBeforeTiledMapLoaded()
    {
    }

    protected virtual void OnTiledMapLoaded(TiledMapInstance map)
    {
    }

    protected virtual void OnTiledMapUnloading(TiledMapInstance map)
    {
    }

    protected virtual void OnTiledMapUnloaded(TmxMap map)
    {
    }

    protected virtual void OnTiledSceneReady()
    {
    }

    protected virtual void OnTiledSceneEnding()
    {
    }
}
