using Dreambit;
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

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, true);
    }
}
