using Dreambit;
using Dreambit.Editor.Assets;
using Dreambit.Editor.Projects;
using DreambitEngine.AssetBaker.Pipeline;

namespace Dreambit.Editor.Tests;

public sealed class AssetBakePipelineTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        "Dreambit.Editor.AssetBakePipelineTests",
        Guid.NewGuid().ToString("N"));

    public AssetBakePipelineTests()
    {
        Directory.CreateDirectory(_root);
    }

    [Fact]
    public void BakeIsDeterministicIncrementalAndEmbedsRuntimeRegistry()
    {
        var assets = Path.Combine(_root, "Assets");
        var cache = Path.Combine(_root, "cache");
        var output = Path.Combine(_root, "Content", "content.pak");
        var registry = Path.Combine(_root, ".dreambit", "assets.json");
        Directory.CreateDirectory(assets);
        Directory.CreateDirectory(Path.GetDirectoryName(registry)!);
        File.WriteAllText(Path.Combine(assets, "hero.sprite.json"), "{\"name\":\"hero\"}");
        var id = Guid.NewGuid();
        File.WriteAllText(
            registry,
            $$"""
              {
                "schemaVersion": 1,
                "assets": [
                  { "id": "{{id:D}}", "path": "hero.sprite.json", "kind": "Sprite" }
                ]
              }
              """);

        var pipeline = new AssetBakePipeline();
        var request = new AssetBakeRequest(assets, output, registry, cache);
        var first = pipeline.BakePak(request);
        var firstBytes = File.ReadAllBytes(output);
        var second = pipeline.BakePak(request);
        var secondBytes = File.ReadAllBytes(output);

        Assert.Equal(1, first.BakedCount);
        Assert.Equal(0, first.CacheHitCount);
        Assert.Equal(0, second.BakedCount);
        Assert.Equal(1, second.CacheHitCount);
        Assert.Equal(firstBytes, secondBytes);

        using var pak = new PakReader(output);
        using var stream = pak.Open(RuntimeAssetRegistry.LogicalPath);
        var runtimeRegistry = RuntimeAssetRegistry.Load(stream);
        Assert.True(runtimeRegistry.TryResolveAssetName(new AssetId(id), out var name));
        Assert.Equal("hero.sprite", name);
    }

    [Fact]
    public void FailedBakeDoesNotReplaceLastKnownGoodPak()
    {
        var assets = Path.Combine(_root, "Assets");
        var output = Path.Combine(_root, "Content", "content.pak");
        Directory.CreateDirectory(assets);
        var source = Path.Combine(assets, "data.json");
        File.WriteAllText(source, "{\"valid\":true}");
        var pipeline = new AssetBakePipeline();
        var request = new AssetBakeRequest(assets, output, RebuildAll: true);
        pipeline.BakePak(request);
        var goodBytes = File.ReadAllBytes(output);

        File.WriteAllText(source, "{ invalid json");
        Assert.ThrowsAny<Exception>(() => pipeline.BakePak(request));
        Assert.Equal(goodBytes, File.ReadAllBytes(output));
    }

    [Fact]
    public void BakeAtomicallyReplacesPakWhileTheEditorIsReadingThePreviousVersion()
    {
        var assets = Path.Combine(_root, "Assets");
        var output = Path.Combine(_root, "Content", "content.pak");
        Directory.CreateDirectory(assets);
        var source = Path.Combine(assets, "data.json");
        File.WriteAllText(source, "{\"version\":1}");
        var pipeline = new AssetBakePipeline();
        var request = new AssetBakeRequest(assets, output, RebuildAll: true);
        pipeline.BakePak(request);

        using var previousPak = new PakReader(output);
        using var previousStream = previousPak.Open("data.jsonb");
        File.WriteAllText(source, "{\"version\":2}");

        pipeline.BakePak(request);

        Assert.NotEqual(-1, previousStream.ReadByte());
        using var replacementPak = new PakReader(output);
        using var replacementStream = replacementPak.Open("data.jsonb");
        Assert.NotEqual(-1, replacementStream.ReadByte());
    }

    [Fact]
    public void SceneAssetsBakeToTheRuntimeNameAcceptedByTheSceneLoader()
    {
        var assets = Path.Combine(_root, "Assets");
        var output = Path.Combine(_root, "Content", "content.pak");
        Directory.CreateDirectory(Path.Combine(assets, "levels"));
        File.WriteAllText(
            Path.Combine(assets, "levels", "first.scene.json"),
            "{\"name\":\"First\",\"entities\":[]}");

        new AssetBakePipeline().BakePak(new AssetBakeRequest(assets, output, RebuildAll: true));

        using (var pak = new PakReader(output))
        using (var stream = pak.Open("levels/first.scene.jsonb"))
            Assert.True(stream.Length > 0);

        var loader = new SceneBlueprintLoader();
        var loaded = Assert.IsType<SceneBlueprint>(
            loader.Load("levels/first", "content.pak", true, Path.GetDirectoryName(output)!));
        Assert.Equal("First", loaded.Name);
        Assert.Equal("levels/first.scene", loaded.AssetName);
    }

    [Fact]
    public void EditorBakesIntoRepositoryCacheInsteadOfLauncherOutput()
    {
        var contentRoot = Path.Combine(_root, "src", "Game.Content", "Assets");
        Directory.CreateDirectory(contentRoot);
        var project = new DreambitProjectDefinition(
            _root,
            Path.Combine(_root, ".dreambit", "project.json"),
            new DreambitProjectMetadata(),
            Path.Combine(_root, "Game.sln"),
            Path.Combine(_root, "src", "Game", "Game.csproj"),
            Path.Combine(_root, "src", "Game.Content", "Game.Content.csproj"),
            contentRoot,
            Path.Combine(_root, "src", "Game.VK", "Game.VK.csproj"));
        using var assets = new AssetDatabase(_root, contentRoot);
        using var baking = new AssetBakeService(project, assets, CancellationToken.None);

        Assert.Equal(
            Path.Combine(_root, ".cache", "dreambit", "content.pak"),
            baking.OutputPakPath);
        Assert.Equal(
            Path.Combine(_root, ".cache", "dreambit", "bake"),
            baking.CacheDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, true);
    }
}
