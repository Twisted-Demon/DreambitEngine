using Dreambit.Editor.Assets;
using Dreambit.Editor.Scenes;

namespace Dreambit.Editor.Tests;

public sealed class BlueprintSourceServiceTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        "Dreambit.Editor.BlueprintSourceServiceTests",
        Guid.NewGuid().ToString("N"));

    private string ContentRoot => Path.Combine(_root, "Content", "Assets");

    public BlueprintSourceServiceTests() => Directory.CreateDirectory(ContentRoot);

    [Fact]
    public void UnsavedPreviewTakesPrecedenceOverDiskSource()
    {
        WriteBlueprint("actors/hero.blueprint.json", "Disk Hero");
        using var assets = new AssetDatabase(_root, ContentRoot, enableWatcher: false);
        var asset = Assert.Single(assets.GetSnapshot().Assets);
        using var sources = new BlueprintSourceService(assets);
        using var preview = new EntityBlueprint { Name = "Unsaved Hero" };

        sources.SetPreview(asset, preview);
        using var loaded = sources.Load(asset);

        Assert.Equal("Unsaved Hero", loaded.Name);
        Assert.Equal(asset.Id, loaded.AssetId);
        Assert.Equal(asset.LogicalAssetName, loaded.AssetName);
        Assert.NotSame(preview, loaded);
    }

    [Fact]
    public void StableIdDoesNotFallBackToAnUnrelatedMatchingName()
    {
        WriteBlueprint("actors/hero.blueprint.json", "Hero");
        using var assets = new AssetDatabase(_root, ContentRoot, enableWatcher: false);
        var asset = Assert.Single(assets.GetSnapshot().Assets);
        using var sources = new BlueprintSourceService(assets);

        var reference = new BlueprintInstanceReference
        {
            AssetId = Guid.NewGuid(),
            AssetName = asset.LogicalAssetName
        };

        var exception = Assert.Throws<FileNotFoundException>(() => sources.Resolve(reference));
        Assert.Contains(reference.AssetId.ToString("D"), exception.Message);
    }

    [Fact]
    public void ClearingOnePreviewMakesSubsequentLoadsReadTheCurrentDiskSource()
    {
        WriteBlueprint("actors/hero.blueprint.json", "Disk Hero");
        using var assets = new AssetDatabase(_root, ContentRoot, enableWatcher: false);
        var asset = Assert.Single(assets.GetSnapshot().Assets);
        using var sources = new BlueprintSourceService(assets);
        using var preview = new EntityBlueprint { Name = "Unsaved Hero" };
        sources.SetPreview(asset, preview);
        WriteBlueprint("actors/hero.blueprint.json", "Externally Rewritten Hero");

        sources.ClearPreview(asset.Id);
        using var loaded = sources.Load(asset);

        Assert.Equal("Externally Rewritten Hero", loaded.Name);
    }

    private void WriteBlueprint(string relativePath, string name)
    {
        var path = Path.Combine(ContentRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, DreambitJson.Serialize(new EntityBlueprint { Name = name }));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, true);
    }
}
