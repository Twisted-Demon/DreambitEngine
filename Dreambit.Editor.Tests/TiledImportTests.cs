using System.Buffers.Binary;
using System.IO.Compression;
using Dreambit.ECS;
using Dreambit.Editor.Graphics;
using Dreambit.Editor.Scenes;
using Dreambit.Tiled;
using DreambitEngine.AssetBaker.Pipeline;
using Microsoft.Xna.Framework;

namespace Dreambit.Editor.Tests;

public sealed class TiledImportTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        "Dreambit.Editor.TiledImportTests",
        Guid.NewGuid().ToString("N"));

    public TiledImportTests() => Directory.CreateDirectory(_root);

    [Fact]
    public void SceneLinkSurvivesCaptureAndReadsLegacyPixelsPerUnit()
    {
        var assetId = Guid.NewGuid();
        var source = new SceneBlueprint
        {
            Name = "Tiled World",
            Tiled = new TiledSceneReference
            {
                AssetId = assetId,
                AssetName = "maps/world",
                ImportOptions = new TiledImportOptions
                {
                    PixelsPerUnit = 16f,
                    BaseDrawLayer = 20,
                    DrawLayerStep = 3,
                    WorldDepth = 2,
                    WorldDepthDrawLayerStride = 400
                }
            }
        };
        using var scene = new TestScene();
        scene.CreateEntity("Dreambit Placed");

        var captured = SceneDocumentSerializer.Capture(scene, source, source.Name);
        var restored = SceneDocumentSerializer.Deserialize(SceneDocumentSerializer.Serialize(captured));

        Assert.NotNull(restored.Tiled);
        Assert.Equal(assetId, restored.Tiled.AssetId);
        Assert.Equal("maps/world", restored.Tiled.AssetName);
        Assert.Equal(16f, restored.Tiled.ImportOptions.PixelsPerUnit);
        Assert.Equal(20, restored.Tiled.ImportOptions.BaseDrawLayer);
        Assert.Equal(3, restored.Tiled.ImportOptions.DrawLayerStep);
        Assert.Equal(2, restored.Tiled.ImportOptions.WorldDepth);
        Assert.Equal(400, restored.Tiled.ImportOptions.WorldDepthDrawLayerStride);
        Assert.Equal("Dreambit Placed", Assert.Single(restored.Entities).Name);

        var legacy = SceneDocumentSerializer.Deserialize("""
        {
          "name": "Legacy Tiled",
          "entities": [],
          "tiled": {
            "asset": "maps/world",
            "pixels_per_unit": 24
          }
        }
        """);
        Assert.Equal(24f, legacy.Tiled!.ImportOptions.PixelsPerUnit);
    }

    [Fact]
    public void SourceLoaderResolvesExternalTilesetsImagesAndAnimations()
    {
        var contentRoot = Path.Combine(_root, "Assets");
        var mapsDirectory = Path.Combine(contentRoot, "maps");
        var tilesetDirectory = Path.Combine(mapsDirectory, "tiles");
        Directory.CreateDirectory(tilesetDirectory);
        var mapPath = Path.Combine(mapsDirectory, "world.tmx");
        var tilesetPath = Path.Combine(tilesetDirectory, "terrain.tsx");
        File.WriteAllText(mapPath, """
        <?xml version="1.0" encoding="UTF-8"?>
        <map version="1.10" tiledversion="1.11.2" orientation="orthogonal" renderorder="right-down" width="2" height="1" tilewidth="16" tileheight="16" infinite="0">
          <tileset firstgid="17" source="tiles/terrain.tsx"/>
          <layer id="1" name="Ground" width="2" height="1">
            <data encoding="csv">17,18</data>
          </layer>
        </map>
        """);
        File.WriteAllText(tilesetPath, """
        <?xml version="1.0" encoding="UTF-8"?>
        <tileset version="1.10" tiledversion="1.11.2" name="Terrain" tilewidth="16" tileheight="16" tilecount="2" columns="2">
          <image source="../../textures/terrain.png" width="32" height="16"/>
          <tile id="0">
            <animation>
              <frame tileid="0" duration="100"/>
              <frame tileid="1" duration="250"/>
            </animation>
          </tile>
        </tileset>
        """);

        var map = TmxMap.FromContentFile(mapPath, "maps/world", contentRoot);
        var reference = Assert.Single(map.Tilesets);
        var tileset = reference.EffectiveTileset;

        Assert.Equal("maps/world", map.AssetName);
        Assert.Equal(17u, reference.FirstGid);
        Assert.Equal(0u, tileset.FirstGid);
        Assert.Equal("maps/tiles/terrain", tileset.AssetName);
        Assert.Equal("textures/terrain", tileset.Image!.ResolvedAssetName);
        var animation = Assert.Single(tileset.Tiles).Animation!;
        Assert.Equal(new[] { 0, 1 }, animation.Frames.Select(frame => frame.TileId));
        Assert.Equal(new[] { 100, 250 }, animation.Frames.Select(frame => frame.DurationMilliseconds));
        Assert.Throws<TiledException>(() =>
            TmxResolver.ResolveRelativeAssetPath("maps/world", "../../outside"));
    }

    [Fact]
    public void AssetBakePreservesTmxReferencesAndTsxAnimations()
    {
        var assets = Path.Combine(_root, "BakeAssets");
        var tilesetDirectory = Path.Combine(assets, "maps", "tiles");
        var output = Path.Combine(_root, "Content", "content.pak");
        Directory.CreateDirectory(tilesetDirectory);
        File.WriteAllText(Path.Combine(assets, "maps", "world.tmx"), """
        <?xml version="1.0" encoding="UTF-8"?>
        <map version="1.10" tiledversion="1.11.2" orientation="orthogonal" renderorder="right-down" width="1" height="1" tilewidth="16" tileheight="16" infinite="0">
          <tileset firstgid="41" source="tiles/terrain.tsx"/>
          <layer id="1" name="Ground" width="1" height="1">
            <data encoding="csv">41</data>
          </layer>
        </map>
        """);
        File.WriteAllText(Path.Combine(tilesetDirectory, "terrain.tsx"), """
        <?xml version="1.0" encoding="UTF-8"?>
        <tileset version="1.10" tiledversion="1.11.2" name="Terrain" tilewidth="16" tileheight="16" tilecount="2" columns="2">
          <image source="../../textures/terrain.png" width="32" height="16"/>
          <tile id="0">
            <animation>
              <frame tileid="0" duration="80"/>
              <frame tileid="1" duration="120"/>
            </animation>
          </tile>
        </tileset>
        """);

        var result = new AssetBakePipeline().BakePak(
            new AssetBakeRequest(assets, output, RebuildAll: true));

        Assert.Equal(2, result.BakedCount);
        using var pak = new PakReader(output);
        using var mapStream = pak.Open("maps/world.xmlb");
        using var tilesetStream = pak.Open("maps/tiles/terrain.xmlb");
        var map = XmlbLoader.Deserialize<TmxMap>(mapStream)!;
        var tileset = XmlbLoader.Deserialize<TmxTileset>(tilesetStream)!;
        Assert.Equal(41u, Assert.Single(map.Tilesets).FirstGid);
        Assert.Equal("tiles/terrain.tsx", Assert.Single(map.Tilesets).Source);
        Assert.Equal(new[] { 80, 120 },
            Assert.Single(tileset.Tiles).Animation!.Frames.Select(frame => frame.DurationMilliseconds));
    }

    [Fact]
    public void DecoderSupportsFixedXmlCsvAndInfiniteChunkCoordinates()
    {
        var fixedMap = new TmxMap
        {
            AssetName = "maps/fixed",
            Orientation = "orthogonal",
            Width = 2,
            Height = 2,
            TileWidth = 16,
            TileHeight = 16
        };
        var encoded = TmxTileDataDecoder.HorizontalFlipFlag |
                      TmxTileDataDecoder.DiagonalFlipFlag |
                      7u;
        var csvLayer = new TmxTileLayer
        {
            Id = 1,
            Name = "CSV",
            Width = 2,
            Height = 2,
            Data = new TmxData { Encoding = "csv", Value = $"0,{encoded},3,4" }
        };

        var csvCells = TmxTileDataDecoder.DecodeLayer(fixedMap, csvLayer);

        Assert.Equal(4, csvCells.Count);
        Assert.Equal(new Point(1, 0), new Point(csvCells[1].X, csvCells[1].Y));
        Assert.Equal(7u, csvCells[1].GlobalTileId);
        Assert.Equal(
            TmxTileFlipFlags.Horizontal | TmxTileFlipFlags.Diagonal,
            csvCells[1].FlipFlags);

        var xmlLayer = new TmxTileLayer
        {
            Id = 2,
            Name = "XML",
            Width = 2,
            Height = 1,
            Data = new TmxData
            {
                Tiles = [new TmxLayerTile { Gid = 5 }, new TmxLayerTile { Gid = 6 }]
            }
        };
        Assert.Equal(new uint[] { 5, 6 },
            TmxTileDataDecoder.DecodeLayer(fixedMap, xmlLayer).Select(cell => cell.GlobalTileId));

        var infinitePath = Path.Combine(_root, "infinite.tmx");
        File.WriteAllText(infinitePath, """
        <?xml version="1.0" encoding="UTF-8"?>
        <map version="1.10" tiledversion="1.11.2" orientation="orthogonal" renderorder="right-down" width="0" height="0" tilewidth="16" tileheight="16" infinite="1">
          <layer id="3" name="Infinite" width="0" height="0">
            <data encoding="csv">
              <chunk x="-2" y="3" width="2" height="1">7,0</chunk>
            </data>
          </layer>
        </map>
        """);
        var infiniteMap = TmxMap.FromFile(infinitePath);
        var infiniteLayer = Assert.IsType<TmxTileLayer>(Assert.Single(infiniteMap.Layers));
        var infiniteCells = TmxTileDataDecoder.DecodeLayer(infiniteMap, infiniteLayer);

        Assert.True(infiniteMap.Infinite);
        Assert.Collection(
            infiniteCells,
            cell =>
            {
                Assert.Equal((-2, 3), (cell.X, cell.Y));
                Assert.Equal(7u, cell.GlobalTileId);
                Assert.Equal((-2, 3), (cell.ChunkX, cell.ChunkY));
            },
            cell =>
            {
                Assert.Equal((-1, 3), (cell.X, cell.Y));
                Assert.Equal(0u, cell.GlobalTileId);
                Assert.Equal((-2, 3), (cell.ChunkX, cell.ChunkY));
            });
    }

    [Fact]
    public void DecoderAcceptsAnEmptyCsvLayerInAnInfiniteMap()
    {
        var map = new TmxMap
        {
            Infinite = true,
            Width = 100,
            Height = 80,
            TileWidth = 32,
            TileHeight = 32
        };
        var layer = new TmxTileLayer
        {
            Name = "Empty",
            Width = 100,
            Height = 80,
            Data = new TmxData { Encoding = "csv", Value = null }
        };

        Assert.Empty(TmxTileDataDecoder.DecodeLayer(map, layer));
    }

    [Fact]
    public void DecoderSupportsUncompressedGzipAndZlibBase64()
    {
        var values = new[]
        {
            1u,
            TmxTileDataDecoder.VerticalFlipFlag | 2u,
            0u,
            9u
        };
        foreach (var compression in new string?[] { null, "gzip", "zlib" })
        {
            var map = new TmxMap
            {
                AssetName = "maps/base64",
                Orientation = "orthogonal",
                Width = 2,
                Height = 2,
                TileWidth = 16,
                TileHeight = 16
            };
            var layer = new TmxTileLayer
            {
                Id = 1,
                Name = compression ?? "uncompressed",
                Width = 2,
                Height = 2,
                Data = new TmxData
                {
                    Encoding = "base64",
                    Compression = compression,
                    Value = EncodeBase64(values, compression)
                }
            };

            var decoded = TmxTileDataDecoder.DecodeLayer(map, layer);

            Assert.Equal(values, decoded.Select(cell => cell.EncodedGlobalTileId));
            Assert.Equal(TmxTileFlipFlags.Vertical, decoded[1].FlipFlags);
            Assert.Equal(2u, decoded[1].GlobalTileId);
        }
    }

    [Fact]
    public void AnimatedTilesLoopUsingFrameDurations()
    {
        var first = new Rectangle(0, 0, 16, 16);
        var second = new Rectangle(16, 0, 16, 16);
        var animation = new TilemapAnimation(
        [
            new TilemapAnimationFrame(first, 100),
            new TilemapAnimationFrame(second, 250)
        ]);

        Assert.Equal(first, animation.GetFrame(0).SourceRectangle);
        Assert.Equal(first, animation.GetFrame(99).SourceRectangle);
        Assert.Equal(second, animation.GetFrame(100).SourceRectangle);
        Assert.Equal(second, animation.GetFrame(349).SourceRectangle);
        Assert.Equal(first, animation.GetFrame(350).SourceRectangle);
        Assert.Equal(second, animation.GetFrame(-1).SourceRectangle);
    }

    [Fact]
    public void SparseTilemapDataDoesNotAllocateTheInfiniteBoundingArea()
    {
        const int columns = 1_000_000;
        const int rows = 1_000_000;
        var cell = new Point(columns - 1, rows - 1);
        var tile = new TilemapTile(
            new Vector2(cell.X, cell.Y),
            Vector2.One,
            new Rectangle(0, 0, 1, 1),
            Color.White,
            Cell: cell);

        var layer = new TilemapLayerData(columns, rows, Vector2.One, [tile]);

        Assert.Equal(1, layer.TileCount);
        Assert.Equal(tile, Assert.Single(layer.GetTiles(cell.X, cell.Y)));
        Assert.Empty(layer.GetTiles(0, 0));
    }

    [Fact]
    public void SparseTilemapVisibilityVisitsOnlyOccupiedIntersectingChunks()
    {
        const int columns = 1_000_000;
        const int rows = 1_000_000;
        var nearby = new TilemapTile(
            new Vector2(10, 10),
            Vector2.One,
            new Rectangle(0, 0, 1, 1),
            Color.White,
            Cell: new Point(10, 10));
        var distant = new TilemapTile(
            new Vector2(columns - 1, rows - 1),
            Vector2.One,
            new Rectangle(0, 0, 1, 1),
            Color.White,
            Cell: new Point(columns - 1, rows - 1));
        var layer = new TilemapLayerData(
            columns,
            rows,
            Vector2.One,
            [nearby, distant]);
        var visible = new List<TilemapChunkData>();

        layer.GetVisibleChunks(new RectangleF(9, 9, 4, 4), visible);

        Assert.Equal(2, layer.ChunkCount);
        var chunk = Assert.Single(visible);
        Assert.Equal(nearby, Assert.Single(chunk.Tiles));
    }

    [Fact]
    public void TilemapLayerPreservesSourceChunksAndSeparatesAnimatedTiles()
    {
        var animation = new TilemapAnimation(
        [
            new TilemapAnimationFrame(new Rectangle(0, 0, 1, 1), 100)
        ]);
        var sourceChunk = new Point(-16, 32);
        var staticTile = new TilemapTile(
            Vector2.Zero,
            Vector2.One,
            new Rectangle(0, 0, 1, 1),
            Color.White,
            Cell: Point.Zero,
            Chunk: sourceChunk);
        var animatedTile = new TilemapTile(
            Vector2.One,
            Vector2.One,
            new Rectangle(0, 0, 1, 1),
            Color.White,
            Animation: animation,
            Cell: new Point(1, 1),
            Chunk: sourceChunk);

        var layer = new TilemapLayerData(2, 2, Vector2.One, [staticTile, animatedTile]);

        var chunk = Assert.Single(layer.Chunks);
        Assert.Equal(sourceChunk, chunk.Coordinate);
        Assert.Equal(staticTile, Assert.Single(chunk.StaticTiles));
        Assert.Equal(animatedTile, Assert.Single(chunk.AnimatedTiles));
    }

    [Fact]
    public void ImporterBuildsTileOnlyHierarchyWithDrawLayerAndInfiniteBoundsOptions()
    {
        using var scene = new TestScene();
        var ground = new TmxTileLayer
        {
            Id = 2,
            Name = "Ground",
            Width = 2,
            Height = 1,
            Data = new TmxData { Encoding = "csv", Value = "0,0" }
        };
        var detail = new TmxTileLayer
        {
            Id = 5,
            Name = "Detail",
            X = 1,
            Width = 1,
            Height = 1,
            Data = new TmxData { Encoding = "csv", Value = "0" }
        };
        var group = new TmxGroupLayer
        {
            Id = 4,
            Name = "Decor",
            OffsetX = 8,
            OffsetY = 4,
            Layers = [detail]
        };
        var map = new TmxMap
        {
            AssetName = "maps/fixed",
            Orientation = "orthogonal",
            Width = 2,
            Height = 1,
            TileWidth = 16,
            TileHeight = 16,
            BackgroundColor = "#80102030",
            Layers =
            [
                new TmxObjectLayer
                {
                    Id = 1,
                    Name = "Objects",
                    Objects = [new TmxObject { Id = 1, Name = "Ignored" }]
                },
                ground,
                new TmxImageLayer { Id = 3, Name = "Image" },
                group
            ]
        };
        var options = new TiledImportOptions
        {
            PixelsPerUnit = 2f,
            BaseDrawLayer = 10,
            DrawLayerStep = 4,
            WorldDepth = 3,
            WorldDepthDrawLayerStride = 100
        };

        var instance = new TiledMapImporter(_ => null).Import(scene, map, options);
        scene.FlushStructuralChanges();

        Assert.Equal(5, instance.OwnedEntities.Count);
        Assert.All(instance.OwnedEntities, entity => Assert.True(entity.IsTiledGenerated));
        Assert.DoesNotContain(instance.OwnedEntities, entity => entity.Name.Contains("Objects"));
        Assert.DoesNotContain(instance.OwnedEntities, entity => entity.Name.Contains("Image"));
        Assert.Equal(314, instance.GetDrawLayer(ground));
        Assert.Equal(318, instance.GetDrawLayer(detail));
        var groupEntity = Assert.Single(instance.OwnedEntities, entity => entity.Name.Contains("Tiled Group"));
        Assert.Equal(new Vector2(4, 2), groupEntity.Transform.Position2D);
        var detailEntity = Assert.Single(instance.OwnedEntities, entity => entity.Name.EndsWith("Decor/Detail"));
        Assert.Same(groupEntity, detailEntity.Parent);
        Assert.Equal(new Vector2(8, 0), detailEntity.Transform.Position2D);
        var background = Assert.Single(
            instance.OwnedEntities.SelectMany(entity => entity.GetAllComponents()).OfType<FilledRectDrawer>());
        Assert.Equal(16f, background.Width);
        Assert.Equal(8f, background.Height);
        Assert.Equal(new Color(0x10, 0x20, 0x30, 0x80), background.Color);
        Assert.Equal(310, background.DrawLayer);

        instance.Unload();
        scene.FlushStructuralChanges();
        Assert.Empty(scene.GetAllEntities());

        using var infiniteScene = new TestScene();
        var infiniteLayer = new TmxTileLayer
        {
            Id = 1,
            Name = "Infinite",
            Data = new TmxData
            {
                Encoding = "csv",
                Chunks = [new TmxChunk { X = -2, Y = 3, Width = 2, Height = 1, Value = "0,0" }]
            }
        };
        var infiniteMap = new TmxMap
        {
            AssetName = "maps/infinite",
            Orientation = "orthogonal",
            Infinite = true,
            TileWidth = 16,
            TileHeight = 16,
            BackgroundColor = "#102030",
            Layers = [infiniteLayer]
        };

        var infiniteInstance = new TiledMapImporter(_ => null).Import(
            infiniteScene,
            infiniteMap,
            new TiledImportOptions { PixelsPerUnit = 2f });
        infiniteScene.FlushStructuralChanges();

        var infiniteEntity = Assert.Single(
            infiniteInstance.OwnedEntities,
            entity => entity.Name.EndsWith("Infinite"));
        Assert.Equal(new Vector2(-16, 24), infiniteEntity.Transform.Position2D);
        var infiniteBackground = Assert.Single(
            infiniteInstance.OwnedEntities.SelectMany(entity => entity.GetAllComponents()).OfType<FilledRectDrawer>());
        Assert.Equal(new Vector2(-16, 24), infiniteBackground.Entity.Transform.Position2D);
        Assert.Equal(16f, infiniteBackground.Width);
        Assert.Equal(8f, infiniteBackground.Height);
    }

    [Fact]
    public void ImporterRejectsNonOrthogonalMapsAndUnsupportedBlendModes()
    {
        using var scene = new TestScene();
        var importer = new TiledMapImporter(_ => null);
        var isometric = new TmxMap
        {
            AssetName = "maps/isometric",
            Orientation = "isometric",
            Width = 1,
            Height = 1,
            TileWidth = 16,
            TileHeight = 16
        };
        Assert.Throws<TiledException>(() => importer.Import(scene, isometric));

        var blended = new TmxMap
        {
            AssetName = "maps/blended",
            Orientation = "orthogonal",
            Width = 1,
            Height = 1,
            TileWidth = 16,
            TileHeight = 16,
            Layers =
            [
                new TmxTileLayer
                {
                    Id = 1,
                    Name = "Multiply",
                    BlendMode = "multiply",
                    Width = 1,
                    Height = 1,
                    Data = new TmxData { Encoding = "csv", Value = "0" }
                }
            ]
        };
        Assert.Throws<TiledException>(() => importer.Import(scene, blended));
    }

    [Fact]
    public void EditorReimportPreservesAuthoredEntitiesAndGeneratedOverrides()
    {
        var contentRoot = Path.Combine(_root, "Assets");
        var mapsDirectory = Path.Combine(contentRoot, "maps");
        Directory.CreateDirectory(mapsDirectory);
        var mapPath = Path.Combine(mapsDirectory, "world.tmx");
        WriteEmptyMap(mapPath, 2, "Ground", "#123456");

        var source = new SceneBlueprint
        {
            Name = "Tiled World",
            Tiled = new TiledSceneReference
            {
                AssetId = Guid.NewGuid(),
                AssetName = "maps/world",
                ImportOptions = new TiledImportOptions { PixelsPerUnit = 2f }
            }
        };
        TmxMap ResolveMap(TiledSceneReference _) =>
            TmxMap.FromContentFile(mapPath, "maps/world", contentRoot);

        using var document = new SceneDocument(
            source,
            null,
            new SelectionService(),
            tiledMapResolver: ResolveMap);
        var generated = document.Scene!.GetAllEntities()
            .Where(entity => entity.IsEditorOnly)
            .ToArray();
        Assert.Equal(3, generated.Length);
        Assert.All(generated, entity => Assert.True(entity.IsTiledGenerated));
        Assert.DoesNotContain(generated, entity => entity.Name.Contains("Objects"));
        var background = Assert.Single(
            generated
                .SelectMany(entity => entity.GetAllComponents())
                .OfType<FilledRectDrawer>());

        Assert.False(
            SceneViewportRenderer.ShouldPickDrawable(
                background));

        Assert.All(
            generated
                .SelectMany(entity => entity.GetAllComponents())
                .OfType<TilemapRenderer>(),
            renderer =>
                Assert.False(
                    SceneViewportRenderer.ShouldPickDrawable(
                        renderer)));
        var placed = document.CreateEmpty("Dreambit Placed");
        var placedId = placed.Id;
        document.Apply("Move Dreambit Entity", _ =>
            placed.Transform.Position = new Vector3(12, 34, 0));
        document.SetEntityPosition([background.Entity], new Vector3(5, 7, 0));
        document.SetEntityEnabled([background.Entity], false);
        document.SetEntityTags([background.Entity], ["editor-override"]);
        document.SetComponentMember(
            "Override Tiled Background Width",
            [background],
            nameof(FilledRectDrawer.Width),
            typeof(float),
            99f,
            (component, value) => ((FilledRectDrawer)component).Width = (float)value!);

        WriteEmptyMap(mapPath, 4, "Ground Updated", "#654321");
        document.ReimportTiled();

        var preserved = document.Scene!.FindEntity(placedId);
        Assert.NotNull(preserved);
        Assert.Equal(new Vector3(12, 34, 0), preserved.Transform.Position);
        Assert.Contains(
            document.Scene.GetAllEntities(),
            entity => entity.Name.EndsWith("Ground Updated"));
        var reimportedBackground = Assert.Single(
            document.Scene.GetAllEntities()
                .SelectMany(entity => entity.GetAllComponents())
                .OfType<FilledRectDrawer>());
        Assert.Equal(new Vector3(5, 7, 0), reimportedBackground.Entity.Transform.Position);
        Assert.False(reimportedBackground.Entity.LocallyEnabled);
        Assert.Contains("editor-override", reimportedBackground.Entity.Tags);
        Assert.Equal(99f, reimportedBackground.Width);

        document.UpdateTiledImportOptions("Disable Tiled Background", options =>
        {
            options.PixelsPerUnit = 16f;
            options.RenderMapBackgroundColor = false;
        });
        Assert.Equal(16f, document.TiledReference!.ImportOptions.PixelsPerUnit);
        Assert.Empty(document.Scene.GetAllEntities()
            .SelectMany(entity => entity.GetAllComponents())
            .OfType<FilledRectDrawer>());
        Assert.NotNull(document.Scene.FindEntity(placedId));

        var validMap = File.ReadAllText(mapPath);
        var workingScene = document.Scene;
        File.WriteAllText(mapPath, "<map incomplete");
        Assert.ThrowsAny<Exception>(() => document.ReimportTiled());
        Assert.Same(workingScene, document.Scene);
        Assert.NotNull(document.Scene.FindEntity(placedId));
        File.WriteAllText(mapPath, validMap);
        document.ReimportTiled();

        var captured = SceneDocumentSerializer.Capture(
            document.Scene,
            new SceneBlueprint
            {
                Name = "Tiled World",
                Tiled = document.TiledReference
            },
            "Tiled World");
        Assert.Single(captured.Entities);
        Assert.Equal("Dreambit Placed", captured.Entities[0].Name);
        Assert.NotEmpty(captured.Tiled!.EntityOverrides);
        var roundTripped = SceneDocumentSerializer.Deserialize(SceneDocumentSerializer.Serialize(captured));
        Assert.Equal(16f, roundTripped.Tiled!.ImportOptions.PixelsPerUnit);
        Assert.Contains(
            roundTripped.Tiled.EntityOverrides.Values,
            item => item.Position == new Vector3(5, 7, 0));
        Assert.Contains(
            roundTripped.Tiled.EntityOverrides.Values,
            item => item.Tags?.Contains("editor-override") == true);
    }

    private static string EncodeBase64(IReadOnlyList<uint> values, string? compression)
    {
        var bytes = new byte[checked(values.Count * sizeof(uint))];
        for (var index = 0; index < values.Count; index++)
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(index * sizeof(uint)), values[index]);
        if (compression is null)
            return Convert.ToBase64String(bytes);

        using var output = new MemoryStream();
        using (Stream compressor = compression switch
               {
                   "gzip" => new GZipStream(output, CompressionLevel.SmallestSize, leaveOpen: true),
                   "zlib" => new ZLibStream(output, CompressionLevel.SmallestSize, leaveOpen: true),
                   _ => throw new ArgumentOutOfRangeException(nameof(compression))
               })
        {
            compressor.Write(bytes);
        }
        return Convert.ToBase64String(output.ToArray());
    }

    private static void WriteEmptyMap(
        string path,
        int width,
        string layerName,
        string backgroundColor)
    {
        var values = string.Join(',', Enumerable.Repeat("0", width));
        File.WriteAllText(path, $$"""
        <?xml version="1.0" encoding="UTF-8"?>
        <map version="1.10" tiledversion="1.11.2" orientation="orthogonal" renderorder="right-down" width="{{width}}" height="1" tilewidth="16" tileheight="16" infinite="0" backgroundcolor="{{backgroundColor}}">
          <objectgroup id="1" name="Objects">
            <object id="1" name="Ignored" x="0" y="0"/>
          </objectgroup>
          <layer id="2" name="{{layerName}}" width="{{width}}" height="1">
            <data encoding="csv">{{values}}</data>
          </layer>
        </map>
        """);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, true);
        }
        catch (IOException)
        {
        }
    }

    private sealed class TestScene : Scene
    {
        public TestScene() : base(SceneExecutionMode.Editor)
        {
        }
    }
}
