using System;
using System.Collections.Generic;
using System.Linq;
using Dreambit.ECS;

namespace Dreambit.Tiled;

/// <summary>
/// Runtime ownership handle for everything materialized from one Tiled TMX map.
/// Disposing it unloads only those generated entities.
/// </summary>
public sealed class TiledMapInstance : IDisposable
{
    private readonly Scene _scene;
    private readonly List<Entity> _ownedEntities;
    private readonly List<TilemapRenderer> _tilemapRenderers;
    private readonly IReadOnlyDictionary<int, int> _layerDrawLayers;

    internal TiledMapInstance(
        Scene scene,
        TmxMap map,
        TiledImportOptions importOptions,
        Entity rootEntity,
        List<Entity> ownedEntities,
        List<TilemapRenderer> tilemapRenderers,
        IReadOnlyDictionary<int, int> layerDrawLayers)
    {
        _scene = scene;
        Map = map;
        ImportOptions = importOptions;
        RootEntity = rootEntity;
        _ownedEntities = ownedEntities;
        _tilemapRenderers = tilemapRenderers;
        _layerDrawLayers = layerDrawLayers;
    }

    public TmxMap Map { get; }
    public TiledImportOptions ImportOptions { get; }
    public float PixelsPerUnit => ImportOptions.PixelsPerUnit;
    public string Identifier => Map.AssetName;
    public Entity RootEntity { get; }
    public IReadOnlyList<Entity> OwnedEntities => _ownedEntities;
    public IReadOnlyList<TilemapRenderer> TilemapRenderers => _tilemapRenderers;
    public bool IsUnloaded { get; private set; }

    public int GetDrawLayer(TmxTileLayer layer)
    {
        ArgumentNullException.ThrowIfNull(layer);
        if (!EnumerateTileLayers(Map.Layers).Any(candidate => ReferenceEquals(candidate, layer)) ||
            !_layerDrawLayers.TryGetValue(layer.Id, out var drawLayer))
        {
            throw new ArgumentException(
                $"Layer '{layer.Name}' does not belong to loaded map '{Identifier}'.",
                nameof(layer));
        }
        return drawLayer;
    }

    public bool TryGetTileLayer(string name, out TmxTileLayer layer)
    {
        layer = EnumerateTileLayers(Map.Layers).FirstOrDefault(candidate =>
            string.Equals(candidate.Name, name, StringComparison.Ordinal));
        return layer is not null;
    }

    public void ApplyDrawLayer(Entity entity, TmxTileLayer layer, bool includeDescendants = true)
    {
        ArgumentNullException.ThrowIfNull(entity);
        if (!ReferenceEquals(entity.Scene, _scene))
            throw new InvalidOperationException("The runtime entity belongs to another scene.");

        var drawLayer = GetDrawLayer(layer);
        ApplyDrawLayer(entity, drawLayer);
        if (!includeDescendants)
            return;
        foreach (var child in entity.GetChildren())
            ApplyDrawLayer(child, drawLayer);
    }

    public void TrackEntity(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        if (IsUnloaded)
            throw new ObjectDisposedException(nameof(TiledMapInstance));
        if (!ReferenceEquals(entity.Scene, _scene))
            throw new InvalidOperationException("Only entities belonging to this map's scene can be tracked.");
        TrackSingleEntity(entity);
        foreach (var child in entity.GetChildren())
            TrackSingleEntity(child);
    }

    public void Unload()
    {
        if (IsUnloaded)
            return;

        for (var index = 0; index < _ownedEntities.Count; index++)
        {
            var ownedEntity = _ownedEntities[index];
            if (Entity.IsNull(ownedEntity))
                continue;
            foreach (var child in ownedEntity.GetChildren())
                if (!Entity.IsNull(child) && ReferenceEquals(child.Scene, _scene))
                    TrackSingleEntity(child);
        }

        IsUnloaded = true;
        for (var index = _ownedEntities.Count - 1; index >= 0; index--)
        {
            var entity = _ownedEntities[index];
            if (Entity.IsNull(entity))
                continue;
            entity.Enabled = false;
            _scene.DestroyEntity(entity);
        }
    }

    public void Dispose()
    {
        Unload();
        GC.SuppressFinalize(this);
    }

    private static IEnumerable<TmxTileLayer> EnumerateTileLayers(IEnumerable<TmxLayer> layers)
    {
        foreach (var layer in layers)
        {
            if (layer is TmxTileLayer tileLayer)
                yield return tileLayer;
            if (layer is not TmxGroupLayer group)
                continue;
            foreach (var child in EnumerateTileLayers(group.Layers))
                yield return child;
        }
    }

    private static void ApplyDrawLayer(Entity entity, int drawLayer)
    {
        foreach (var component in entity.GetAllComponents())
            if (component is DrawableComponent drawable)
                drawable.DrawLayer = drawLayer;
    }

    private void TrackSingleEntity(Entity entity)
    {
        if (!ReferenceEquals(entity.Scene, _scene))
            throw new InvalidOperationException("Only entities belonging to this map's scene can be tracked.");
        if (!_ownedEntities.Contains(entity))
            _ownedEntities.Add(entity);
    }
}
