using System;
using System.Collections.Generic;
using System.Linq;
using Dreambit.ECS;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dreambit.LDtk;

/// <summary>
/// Converts raw LDtk level models into Dreambit-native rendering components.
/// LDtk entity instances are collected on the returned handle but deliberately
/// left for the game's entity-generation hook.
/// </summary>
public sealed class LDtkLevelImporter
{
    public LDtkLevelInstance Import(
        Scene scene,
        LDtkLoadedWorld world,
        LDtkLevel level,
        LDtkImportOptions options = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        var worldPosition = world.GetLevelWorldPosition(level.Iid);
        return Import(scene, level, options, worldPosition);
    }

    public LDtkLevelInstance Import(
        Scene scene,
        LDtkLevel level,
        LDtkImportOptions options = null)
        => Import(scene, level, options, new LdtkPoint(level.WorldX, level.WorldY));

    private LDtkLevelInstance Import(
        Scene scene,
        LDtkLevel level,
        LDtkImportOptions options,
        LdtkPoint worldPosition)
    {
        ArgumentNullException.ThrowIfNull(scene);
        ArgumentNullException.ThrowIfNull(level);
        options ??= new LDtkImportOptions();
        options.Validate();

        if (level.Project is null)
            throw new LdtkException($"LDtk level '{level.Identifier}' is not attached to a project.");
        if (level.LayerInstances is null)
            throw new LdtkException(
                $"LDtk level '{level.Identifier}' is still an external-level stub. Load it through LDtkLoadedWorld first.");

        var ownedEntities = new List<Entity>();
        var tilemapRenderers = new List<TilemapRenderer>();
        var layerDrawLayers = new Dictionary<Guid, int>();
        var pixelsPerUnit = options.PixelsPerUnit;
        var levelOrigin = worldPosition.ToWorldVector3(pixelsPerUnit);
        var root = scene.CreateEntity(
            $"LDtk Level: {level.Identifier}",
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "ldtk", "ldtk-level" },
            createAt: levelOrigin);
        root.LDtkSourceKey = LDtkGeneratedEntityKeys.Level(level.Iid);
        ownedEntities.Add(root);

        try
        {
            var depthBase = checked(
                options.BaseDrawLayer + level.WorldDepth * options.WorldDepthDrawLayerStride);
            if (options.RenderLevelBackgroundColor)
                ImportBackgroundColor(scene, root, level, options, depthBase, ownedEntities);
            if (options.RenderLevelBackgroundImage)
                ImportBackgroundImage(scene, root, level, options, depthBase, ownedEntities);

            var layers = level.LayerInstances;
            for (var layerIndex = layers.Length - 1; layerIndex >= 0; layerIndex--)
            {
                var layer = layers[layerIndex];
                if (layer is null)
                    throw new LdtkException(
                        $"LDtk level '{level.Identifier}' contains a null layer instance.");

                var drawLayer = GetLayerDrawLayer(
                    level,
                    layer,
                    layerIndex,
                    options,
                    depthBase);
                if (!layerDrawLayers.TryAdd(layer.Iid, drawLayer))
                    throw new LdtkException(
                        $"LDtk level '{level.Identifier}' contains duplicate layer IID '{layer.Iid}'.");

                if (!layer.Visible && !options.IncludeInvisibleLayers)
                    continue;

                var layerEntity = CreateLayerEntity(
                    scene,
                    root,
                    level,
                    layer,
                    options,
                    ownedEntities);
                if (!HasTiles(layer))
                    continue;

                var renderer = ImportTileLayerRenderer(
                    layerEntity,
                    level,
                    layer,
                    options,
                    drawLayer);
                tilemapRenderers.Add(renderer);
            }

            var entityInstances = layers
                .Where(layer => layer.Visible || options.IncludeInvisibleLayers)
                .SelectMany(layer => layer.EntityInstances ?? [])
                .ToArray();
            var entityIids = new HashSet<Guid>();
            foreach (var entityInstance in entityInstances)
            {
                if (entityInstance is null)
                    throw new LdtkException(
                        $"LDtk level '{level.Identifier}' contains a null entity instance.");
                if (!entityIids.Add(entityInstance.Iid))
                    throw new LdtkException(
                        $"LDtk level '{level.Identifier}' contains duplicate entity IID '{entityInstance.Iid}'.");
            }

            return new LDtkLevelInstance(
                scene,
                level,
                options,
                root,
                ownedEntities,
                tilemapRenderers,
                layerDrawLayers,
                entityInstances);
        }
        catch
        {
            for (var index = ownedEntities.Count - 1; index >= 0; index--)
                scene.DestroyEntity(ownedEntities[index]);
            throw;
        }
    }

    public TilemapLayerData CreateTilemapLayerData(LayerInstance layer, float pixelsPerUnit = 1f)
    {
        ArgumentNullException.ThrowIfNull(layer);
        if (!float.IsFinite(pixelsPerUnit) || pixelsPerUnit <= 0f)
            throw new ArgumentOutOfRangeException(nameof(pixelsPerUnit));
        if (layer._GridSize <= 0 || layer._CWid <= 0 || layer._CHei <= 0)
            throw new LdtkException($"LDtk layer '{layer._Identifier}' has invalid grid dimensions.");

        var gridSize = layer._GridSize;
        var worldTileSize = new Vector2(gridSize / pixelsPerUnit);
        var opacity = MathHelper.Clamp(layer._Opacity, 0f, 1f);
        var tiles = EnumerateTiles(layer).Select(tile => new TilemapTile(
            tile.Px.ToWorldVector2(pixelsPerUnit),
            worldTileSize,
            tile.ToSourceRectangle(gridSize),
            tile.ToTint(opacity),
            tile.ToSpriteEffects()));

        return new TilemapLayerData(
            layer._CWid,
            layer._CHei,
            worldTileSize,
            tiles);
    }

    private Entity CreateLayerEntity(
        Scene scene,
        Entity root,
        LDtkLevel level,
        LayerInstance layer,
        LDtkImportOptions options,
        List<Entity> ownedEntities)
    {
        var layerEntity = CreateChild(
            scene,
            root,
            $"LDtk Layer: {level.Identifier}/{layer._Identifier}",
            new Vector2(
                layer._PxTotalOffsetX / options.PixelsPerUnit,
                layer._PxTotalOffsetY / options.PixelsPerUnit),
            "ldtk-layer");
        layerEntity.LDtkSourceKey = LDtkGeneratedEntityKeys.Layer(level.Iid, layer.Iid);
        ownedEntities.Add(layerEntity);
        return layerEntity;
    }

    private TilemapRenderer ImportTileLayerRenderer(
        Entity layerEntity,
        LDtkLevel level,
        LayerInstance layer,
        LDtkImportOptions options,
        int drawLayer)
    {
        var assetName = layer.TilesetAssetName ?? layer.Tileset?.AssetName;
        if (string.IsNullOrWhiteSpace(assetName))
            throw new LdtkException(
                $"LDtk layer '{layer._Identifier}' in level '{level.Identifier}' has tiles but no external tileset asset.");

        var texture = Resources.LoadAsset<Texture2D>(assetName)
                      ?? throw new LdtkException(
                          $"Could not load tileset texture '{assetName}' for LDtk layer '{layer._Identifier}'.");

        var renderer = layerEntity.AttachComponent<TilemapRenderer>();
        renderer.Configure(texture, CreateTilemapLayerData(layer, options.PixelsPerUnit));
        renderer.DrawLayer = drawLayer;
        return renderer;
    }

    private static void ImportBackgroundColor(
        Scene scene,
        Entity root,
        LDtkLevel level,
        LDtkImportOptions options,
        int depthBase,
        List<Entity> ownedEntities)
    {
        if (level.PxWid <= 0 || level.PxHei <= 0)
            return;

        var entity = CreateChild(
            scene,
            root,
            $"LDtk Background Color: {level.Identifier}",
            Vector2.Zero,
            "ldtk-background");
        entity.LDtkSourceKey = LDtkGeneratedEntityKeys.BackgroundColor(level.Iid);
        ownedEntities.Add(entity);
        var rectangle = entity.AttachComponent<FilledRectDrawer>();
        rectangle.Width = level.PxWid / options.PixelsPerUnit;
        rectangle.Height = level.PxHei / options.PixelsPerUnit;
        rectangle.Color = level._BgColor.ToColor();
        rectangle.DrawLayer = depthBase;
    }

    private static void ImportBackgroundImage(
        Scene scene,
        Entity root,
        LDtkLevel level,
        LDtkImportOptions options,
        int depthBase,
        List<Entity> ownedEntities)
    {
        if (string.IsNullOrWhiteSpace(level.BackgroundAssetName))
            return;

        var texture = Resources.LoadAsset<Texture2D>(level.BackgroundAssetName)
                      ?? throw new LdtkException(
                          $"Could not load background texture '{level.BackgroundAssetName}' for LDtk level '{level.Identifier}'.");
        var position = Vector2.Zero;
        var scale = Vector2.One;
        var source = new Rectangle(0, 0, texture.Width, texture.Height);
        if (level._BgPos is { } backgroundPosition)
        {
            position = backgroundPosition.TopLeftPx.ToWorldVector2(options.PixelsPerUnit);
            scale = backgroundPosition.Scale.ToVector2();
            if (backgroundPosition.CropRect is { Length: >= 4 })
                source = backgroundPosition.ToCropRectangle();
        }

        var entity = CreateChild(
            scene,
            root,
            $"LDtk Background Image: {level.Identifier}",
            position,
            "ldtk-background");
        entity.LDtkSourceKey = LDtkGeneratedEntityKeys.BackgroundImage(level.Iid);
        ownedEntities.Add(entity);
        entity.Transform.Scale2D = scale;
        var drawer = entity.AttachComponent<SpriteDrawer>()
            .SetSprite(Sprite.Create(texture, source, options.PixelsPerUnit))
            .WithPivot(Vector2.Zero);
        drawer.DrawLayer = checked(depthBase + options.DrawLayerStep);
    }

    private static Entity CreateChild(
        Scene scene,
        Entity parent,
        string name,
        Vector2 localPosition,
        string tag)
    {
        var entity = scene.CreateEntity(
            name,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "ldtk", tag });
        entity.Parent = parent;
        entity.Transform.Position2D = localPosition;
        entity.Transform.ResetLastWorldPosition();
        return entity;
    }

    private static int GetLayerDrawLayer(
        LDtkLevel level,
        LayerInstance layer,
        int fallbackIndex,
        LDtkImportOptions options,
        int depthBase)
    {
        var definitions = level.Project.Defs?.Layers ?? [];
        var definitionIndex = Array.FindIndex(definitions, definition => definition.Uid == layer.LayerDefUid);
        var index = definitionIndex >= 0 ? definitionIndex : fallbackIndex;
        var layerCount = Math.Max(definitions.Length, level.LayerInstances?.Length ?? 0);
        var backToFrontIndex = layerCount - index + 1;
        return checked(depthBase + backToFrontIndex * options.DrawLayerStep);
    }

    private static bool HasTiles(LayerInstance layer)
        => (layer.GridTiles?.Length ?? 0) > 0 || (layer.AutoLayerTiles?.Length ?? 0) > 0;

    private static IEnumerable<TileInstance> EnumerateTiles(LayerInstance layer)
    {
        foreach (var tile in layer.GridTiles ?? [])
            yield return tile;
        foreach (var tile in layer.AutoLayerTiles ?? [])
            yield return tile;
    }

}
