using Dreambit;
using Dreambit.Editor.Assets;
using Dreambit.Editor.Projects;
using DreambitEngine.AssetBaker.Pipeline;
using DreambitEngine.AssetBaker.Pipeline.Textures;
using Newtonsoft.Json.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

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
        File.WriteAllText(Path.Combine(assets, "hero.sprite"), "{\"name\":\"hero\"}");
        var id = Guid.NewGuid();
        File.WriteAllText(
            registry,
            $$"""
              {
                "schemaVersion": 1,
                "assets": [
                  { "id": "{{id:D}}", "path": "hero.sprite", "kind": "Sprite" }
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
    public void BlobBakeWritesNoPakAndCanBeLoadedByLogicalPath()
    {
        var assets = Path.Combine(_root, "BlobAssets");
        var cache = Path.Combine(_root, "BlobCache");
        Directory.CreateDirectory(Path.Combine(assets, "levels"));
        File.WriteAllText(
            Path.Combine(assets, "levels", "first.scene"),
            "{\"name\":\"From blobs\",\"entities\":[]}");

        var result = new AssetBakePipeline().BakeBlobs(new AssetBlobBakeRequest(
            assets,
            cache,
            RebuildAll: true));

        Assert.True(File.Exists(result.ManifestPath));
        Assert.True(File.Exists(Path.Combine(cache, BlobContentManifest.FingerprintFileName)));
        Assert.False(File.Exists(Path.Combine(cache, "content.pak")));

        var originalMode = Resources.ContentMode;
        try
        {
            Resources.SetBlobContentSource(cache);
            var loaded = Assert.IsType<SceneBlueprint>(
                new SceneBlueprintLoader().Load(
                    "levels/first.scene",
                    "content.pak",
                    Resources.UsePak,
                    Resources.ActiveContentDirectory));
            Assert.Equal("From blobs", loaded.Name);

            Resources.RefreshContent();
            Resources.ContentMode = AssetContentMode.Auto;
            var autoLoaded = Assert.IsType<SceneBlueprint>(
                new SceneBlueprintLoader().Load(
                    "levels/first.scene",
                    "content.pak",
                    Resources.UsePak,
                    Resources.ActiveContentDirectory));
            Assert.Equal("From blobs", autoLoaded.Name);
        }
        finally
        {
            Resources.ResetContentSource();
            Resources.ContentMode = originalMode;
        }
    }

    [Fact]
    public void IncrementalBlobBakeDoesNotMaterializeCachedPayloads()
    {
        const int payloadLength = 8 * 1024 * 1024;
        var assets = Path.Combine(_root, "LargeBlobAssets");
        var cache = Path.Combine(_root, "LargeBlobCache");
        Directory.CreateDirectory(assets);
        File.WriteAllText(
            Path.Combine(assets, "large.txt"),
            new string('x', payloadLength));

        var pipeline = new AssetBakePipeline();
        var request = new AssetBlobBakeRequest(assets, cache, RebuildAll: false);
        var first = pipeline.BakeBlobs(request);

        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var second = pipeline.BakeBlobs(request);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

        Assert.Equal(1, first.BakedCount);
        Assert.Equal(1, second.CacheHitCount);
        Assert.Equal(first.OutputLength, second.OutputLength);
        Assert.Equal(first.ContentFingerprint, second.ContentFingerprint);
        Assert.True(
            allocated < payloadLength / 2,
            $"A cached {payloadLength:N0}-byte blob allocated {allocated:N0} bytes while baking.");
    }

    [Fact]
    public void PakFingerprintBecomesStaleWhenBlobsChange()
    {
        var assets = Path.Combine(_root, "FingerprintAssets");
        var cache = Path.Combine(_root, "FingerprintCache");
        var output = Path.Combine(_root, "FingerprintContent", "content.pak");
        Directory.CreateDirectory(assets);
        var source = Path.Combine(assets, "data.json");
        File.WriteAllText(source, "{\"version\":1}");
        var pipeline = new AssetBakePipeline();

        pipeline.BakePak(new AssetBakeRequest(
            assets,
            output,
            CacheDirectory: cache,
            RebuildAll: true));
        Assert.Equal(
            File.ReadAllText(Path.Combine(cache, BlobContentManifest.FingerprintFileName)).Trim(),
            File.ReadAllText(output + ".fingerprint").Trim());

        File.WriteAllText(source, "{\"version\":2}");
        pipeline.BakeBlobs(new AssetBlobBakeRequest(assets, cache));

        Assert.NotEqual(
            File.ReadAllText(Path.Combine(cache, BlobContentManifest.FingerprintFileName)).Trim(),
            File.ReadAllText(output + ".fingerprint").Trim());
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
    public void GenericJsonBakePreservesDreambitTypeMetadata()
    {
        var assets = Path.Combine(_root, "CustomAssets");
        var output = Path.Combine(_root, "CustomContent", "content.pak");
        Directory.CreateDirectory(assets);
        File.WriteAllText(
            Path.Combine(assets, "weapon.asset"),
            "{\"$dreambitType\":\"test.custom-asset\",\"Health\":100}");

        new AssetBakePipeline().BakePak(new AssetBakeRequest(assets, output, RebuildAll: true));

        using var pak = new PakReader(output);
        using var stream = pak.Open("weapon.asset.jsonb");
        var baked = JObject.Parse(JsnbLoader.GetJsonString(stream));
        Assert.Equal("test.custom-asset", baked.Value<string>("$dreambitType"));
        Assert.Equal(100, baked.Value<int>("Health"));
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
            Path.Combine(assets, "levels", "first.scene"),
            "{\"name\":\"First\",\"entities\":[]}");

        new AssetBakePipeline().BakePak(new AssetBakeRequest(assets, output, RebuildAll: true));

        using (var pak = new PakReader(output))
        using (var stream = pak.Open("levels/first.scene.jsonb"))
            Assert.True(stream.Length > 0);

        var originalMode = Resources.ContentMode;
        SceneBlueprint loaded;
        try
        {
            // An explicit PAK load must remain independent of an editor preview
            // temporarily using loose files or incremental blobs.
            Resources.ContentMode = AssetContentMode.LooseFiles;
            var loader = new SceneBlueprintLoader();
            loaded = Assert.IsType<SceneBlueprint>(
                loader.Load("levels/first.scene", "content.pak", true, Path.GetDirectoryName(output)!));
        }
        finally
        {
            Resources.ContentMode = originalMode;
        }
        Assert.Equal("First", loaded.Name);
        Assert.Equal("levels/first.scene", loaded.AssetName);
    }

    [Fact]
    public void CutsceneAssetsKeepTheirSemanticExtensionWhenBakedAndLoaded()
    {
        var assets = Path.Combine(_root, "CutsceneAssets");
        var output = Path.Combine(_root, "CutsceneContent", "content.pak");
        Directory.CreateDirectory(assets);
        File.WriteAllText(
            Path.Combine(assets, "intro.cutscene"),
            "- scriptGroup:\n    - script: IntroAction\n");

        new AssetBakePipeline().BakePak(new AssetBakeRequest(assets, output, RebuildAll: true));

        using (var pak = new PakReader(output))
        using (var stream = pak.Open("intro.cutscene.yamlb"))
            Assert.True(stream.Length > 0);

        var loader = new CutsceneLoader();
        var loaded = Assert.IsType<Dreambit.Scripting.Cutscene>(
            loader.Load("intro.cutscene", "content.pak", true, Path.GetDirectoryName(output)!));
        Assert.Equal("intro.cutscene", loaded.AssetName);
        Assert.Equal("IntroAction", Assert.Single(Assert.Single(loaded.Groups).Actions).Script);
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

    [Fact]
    public void TextureBakeDefaultsToLinearPremultipliedSrgbPixels()
    {
        var assets = Path.Combine(_root, "TextureAssets");
        Directory.CreateDirectory(assets);
        var source = Path.Combine(assets, "pixel.png");
        using (var image = new Image<Rgba32>(1, 1))
        {
            image[0, 0] = new Rgba32(200, 100, 50, 128);
            image.SaveAsPng(source);
        }

        var blob = new TextureBaker().BakeToBytes(new DreambitEngine.AssetBaker.Abstractions.BakeContext
        {
            InputPath = source,
            OutputPath = string.Empty,
            LogicalRoot = assets,
            PremultiplyAlpha = true
        });

        Assert.Equal("pixel.texb", blob.LogicalPath);
        Assert.Equal(3u, BitConverter.ToUInt32(blob.Data, 12));
        Assert.Equal(new byte[] { 147, 72, 34, 128 }, blob.Data[20..24]);
    }

    [Fact]
    public void BuiltInEffectsAndFontAreCompiledIntoThePak()
    {
        var assets = Path.Combine(_root, "EmptyAssets");
        var output = Path.Combine(_root, "BuiltIns", "content.pak");
        var cache = Path.Combine(_root, "BuiltIns", "cache");
        Directory.CreateDirectory(assets);
        var legacyEffects = Path.Combine(assets, "Effects");
        Directory.CreateDirectory(legacyEffects);
        File.WriteAllText(
            Path.Combine(legacyEffects, "Tint.fx"),
            "This copied legacy engine effect must never be compiled.");

        new AssetBakePipeline().BakePak(new AssetBakeRequest(
            assets,
            output,
            CacheDirectory: cache,
            RebuildAll: true,
            TargetPlatform: "DesktopVK",
            IncludeBuiltInContent: true));

        using var pak = new PakReader(output);
        using var effect = pak.Open("effects/forwarddiffuse.fxb");
        using var present = pak.Open("effects/present.fxb");
        using var deferred = pak.Open("effects/defferedrendercombine.fxb");
        using var tint = pak.Open("effects/tint.fxb");
        using var font = pak.Open("fonts/monogram.ttfb");
        //Assert.True(effect.Length > 16);
        //Assert.True(present.Length > 16);
        //Assert.True(deferred.Length > 16);
        //Assert.True(tint.Length > 16);
        //Assert.True(font.Length > 16);
        //Assert.True(AssetBakePipeline.HasCurrentBuiltInContent(cache));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, true);
    }
}
