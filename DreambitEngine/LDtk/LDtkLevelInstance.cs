using System;
using System.Collections.Generic;
using System.Linq;
using Dreambit.ECS;
using Microsoft.Xna.Framework;

namespace Dreambit.LDtk;

/// <summary>
/// Runtime ownership handle for everything materialized from one LDtk level.
/// Disposing it unloads only those entities, leaving the containing scene and
/// its persistent entities intact.
/// </summary>
public sealed class LDtkLevelInstance : IDisposable
{
    private readonly Scene _scene;
    private readonly List<Entity> _ownedEntities;
    private readonly List<TilemapRenderer> _tilemapRenderers;
    private readonly IReadOnlyDictionary<Guid, int> _layerDrawLayers;

    internal LDtkLevelInstance(
        Scene scene,
        LDtkLevel level,
        LDtkImportOptions importOptions,
        Entity rootEntity,
        List<Entity> ownedEntities,
        List<TilemapRenderer> tilemapRenderers,
        IReadOnlyDictionary<Guid, int> layerDrawLayers,
        IReadOnlyList<EntityInstance> entityInstances)
    {
        _scene = scene;
        Level = level;
        ImportOptions = importOptions;
        RootEntity = rootEntity;
        _ownedEntities = ownedEntities;
        _tilemapRenderers = tilemapRenderers;
        _layerDrawLayers = layerDrawLayers;
        EntityInstances = entityInstances;
    }

    public LDtkLevel Level { get; }
    public LDtkImportOptions ImportOptions { get; }
    public float PixelsPerUnit => ImportOptions.PixelsPerUnit;
    public Guid Iid => Level.Iid;
    public string Identifier => Level.Identifier;
    public Entity RootEntity { get; }
    public IReadOnlyList<Entity> OwnedEntities => _ownedEntities;
    public IReadOnlyList<TilemapRenderer> TilemapRenderers => _tilemapRenderers;
    public IReadOnlyList<EntityInstance> EntityInstances { get; }
    public bool IsUnloaded { get; private set; }

    /// <summary>Returns the Dreambit draw layer assigned to an LDtk layer.</summary>
    public int GetDrawLayer(LayerInstance layer)
    {
        ArgumentNullException.ThrowIfNull(layer);
        if (!ReferenceEquals(layer.Level, Level) ||
            !_layerDrawLayers.TryGetValue(layer.Iid, out var drawLayer))
            throw new ArgumentException(
                $"Layer '{layer._Identifier}' does not belong to loaded level '{Identifier}'.",
                nameof(layer));
        return drawLayer;
    }

    public bool TryGetLayerInstance(string identifier, out LayerInstance layer)
    {
        layer = Level.LayerInstances?.FirstOrDefault(layer => layer._Identifier == identifier);

        return  layer != null;
    }

    /// <summary>Returns the Dreambit draw layer assigned to an entity's LDtk layer.</summary>
    public int GetDrawLayer(EntityInstance entityInstance)
        => GetDrawLayer(GetOwningLayer(entityInstance));

    /// <summary>
    /// Returns the entity pivot in level-local Dreambit world units, including
    /// the owning layer's total offset.
    /// </summary>
    public Vector2 GetLocalPosition(EntityInstance entityInstance)
    {
        var layer = GetOwningLayer(entityInstance);
        return new Vector2(
            entityInstance.Px.X + layer._PxTotalOffsetX,
            entityInstance.Px.Y + layer._PxTotalOffsetY) / PixelsPerUnit;
    }

    /// <summary>Returns the entity pivot after the level root transform is applied.</summary>
    public Vector2 GetWorldPosition(EntityInstance entityInstance)
        => RootEntity.Transform.TransformPoint2D(GetLocalPosition(entityInstance));

    /// <summary>Creates scaled entity data with owning-layer and draw-layer context.</summary>
    public LDtkEntity CreateEntityData(EntityInstance entityInstance)
    {
        var layer = GetOwningLayer(entityInstance);
        return LDtkEntity.FromInstance(
            entityInstance,
            layer,
            PixelsPerUnit,
            GetDrawLayer(layer));
    }

    /// <summary>
    /// Applies an entity instance's LDtk draw layer to drawable components on
    /// a runtime entity and, by default, its existing descendants.
    /// </summary>
    public void ApplyDrawLayer(
        Entity entity,
        EntityInstance entityInstance,
        bool includeDescendants = true)
    {
        ArgumentNullException.ThrowIfNull(entity);
        if (!ReferenceEquals(entity.Scene, _scene))
            throw new InvalidOperationException("The runtime entity belongs to another scene.");

        var drawLayer = GetDrawLayer(entityInstance);
        ApplyDrawLayer(entity, drawLayer);
        if (!includeDescendants)
            return;

        foreach (var child in entity.GetChildren())
            ApplyDrawLayer(child, drawLayer);
    }

    /// <summary>
    /// Adds a game-created entity to this level's lifetime. Entity-generation
    /// hooks can use this so streamed entities disappear with their level.
    /// </summary>
    public void TrackEntity(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        if (IsUnloaded)
            throw new ObjectDisposedException(nameof(LDtkLevelInstance));
        if (!ReferenceEquals(entity.Scene, _scene))
            throw new InvalidOperationException("Only entities belonging to this level's scene can be tracked.");
        TrackSingleEntity(entity);
        foreach (var child in entity.GetChildren())
            TrackSingleEntity(child);
    }

    public void Unload()
    {
        if (IsUnloaded)
            return;

        // Capture descendants added after TrackEntity was called. This keeps
        // dynamically extended blueprint hierarchies inside the level lifetime.
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
            if (!Entity.IsNull(entity))
            {
                // Entity destruction is processed at the end of an ECS tick.
                // Disable immediately so a streamed-out level cannot render or
                // update for one additional frame while waiting in that queue.
                entity.Enabled = false;
                _scene.DestroyEntity(entity);
            }
        }
    }

    public void Dispose()
    {
        Unload();
        GC.SuppressFinalize(this);
    }

    private LayerInstance GetOwningLayer(EntityInstance entityInstance)
    {
        ArgumentNullException.ThrowIfNull(entityInstance);
        var layer = entityInstance.Layer;
        if (layer is null ||
            !ReferenceEquals(entityInstance.Level, Level) ||
            !ReferenceEquals(layer.Level, Level))
            throw new ArgumentException(
                $"Entity '{entityInstance._Identifier}' does not belong to loaded level '{Identifier}'.",
                nameof(entityInstance));
        return layer;
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
            throw new InvalidOperationException("Only entities belonging to this level's scene can be tracked.");
        if (!_ownedEntities.Contains(entity))
            _ownedEntities.Add(entity);
    }
}
