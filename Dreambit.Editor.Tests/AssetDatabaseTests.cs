using Dreambit;
using Dreambit.Editor.Assets;
using Newtonsoft.Json.Linq;

namespace Dreambit.Editor.Tests;

public sealed class AssetDatabaseTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        "Dreambit.Editor.AssetDatabaseTests",
        Guid.NewGuid().ToString("N"));

    private string ContentRoot => Path.Combine(_root, "Content", "Assets");

    public AssetDatabaseTests()
    {
        Directory.CreateDirectory(ContentRoot);
    }

    [Fact]
    public void InitialScanCreatesStableRegistryAndClassifiesSemanticTypes()
    {
        WriteAsset("characters/hero.sprite.json", "{\"source\":{}}");
        WriteAsset("characters/hero.texture.png", "not-a-real-png");

        AssetId spriteId;
        using (var database = CreateDatabase())
        {
            var snapshot = database.GetSnapshot();
            Assert.Equal(2, snapshot.Assets.Count);
            var sprite = Assert.Single(
                snapshot.Assets,
                asset => asset.RelativePath == "characters/hero.sprite.json");
            Assert.Equal(AssetKind.Sprite, sprite.Kind);
            Assert.Equal("Dreambit.Sprite", sprite.TypeName);
            Assert.Equal("characters/hero.sprite", sprite.LogicalAssetName);
            Assert.False(sprite.Id.IsEmpty);
            spriteId = sprite.Id;
            Assert.True(File.Exists(database.RegistryPath));
        }

        using var reopened = CreateDatabase();
        Assert.True(reopened.TryGetAsset("characters/hero.sprite.json", out var reopenedSprite));
        Assert.Equal(spriteId, reopenedSprite!.Id);
    }

    [Fact]
    public void EditorRenameAndFolderMovePreserveIdsAndRuntimeNames()
    {
        WriteAsset("characters/hero/hero.blueprint.json", "{}");
        WriteAsset("characters/hero/hero.sprite.json", "{}");
        using var database = CreateDatabase();
        var before = database.GetSnapshot().Assets.ToDictionary(
            asset => asset.Name,
            asset => asset.Id);

        Assert.True(database.TryRename(
            "characters/hero/hero.sprite.json",
            "player.sprite.json",
            out var renameError), renameError);
        Assert.True(database.TryCreateFolder("", "actors", out var createError), createError);
        Assert.True(database.TryMove("characters/hero", "actors", out var moveError), moveError);

        Assert.True(database.TryGetAsset(
            "actors/hero/player.sprite.json",
            out var movedSprite));
        Assert.True(database.TryGetAsset(
            "actors/hero/hero.blueprint.json",
            out var movedBlueprint));
        Assert.Equal(before["hero.sprite.json"], movedSprite!.Id);
        Assert.Equal(before["hero.blueprint.json"], movedBlueprint!.Id);
        Assert.True(database.TryResolveAssetName(movedSprite.Id, out var logicalName));
        Assert.Equal("actors/hero/player.sprite", logicalName);
    }

    [Fact]
    public void ExternalMoveIsRecoveredByFingerprintAcrossSessions()
    {
        WriteAsset("old/hero.animation.json", "{\"frames\":[1,2,3]}");
        AssetId originalId;
        using (var database = CreateDatabase())
            originalId = Assert.Single(database.GetSnapshot().Assets).Id;

        Directory.CreateDirectory(Path.Combine(ContentRoot, "new"));
        File.Move(
            Path.Combine(ContentRoot, "old", "hero.animation.json"),
            Path.Combine(ContentRoot, "new", "hero.animation.json"));

        using var reopened = CreateDatabase();
        var moved = Assert.Single(reopened.GetSnapshot().Assets);
        Assert.Equal("new/hero.animation.json", moved.RelativePath);
        Assert.Equal(originalId, moved.Id);
    }

    [Fact]
    public void DeleteLeavesTombstoneAndRestoringThePathRestoresTheId()
    {
        WriteAsset("audio/hit.soundcue.json", "{\"takes\":[]}");
        using var database = CreateDatabase();
        var id = Assert.Single(database.GetSnapshot().Assets).Id;

        Assert.True(database.TryDelete("audio/hit.soundcue.json", out var deleteError), deleteError);
        Assert.Empty(database.GetSnapshot().Assets);
        Assert.Equal(1, database.GetSnapshot().MissingAssetCount);
        Assert.True(database.TryResolveAssetName(id, out var missingName));
        Assert.Equal("audio/hit.soundcue", missingName);

        WriteAsset("audio/hit.soundcue.json", "{\"takes\":[]}");
        database.RefreshNow();
        Assert.Equal(id, Assert.Single(database.GetSnapshot().Assets).Id);
        Assert.Equal(0, database.GetSnapshot().MissingAssetCount);
    }

    [Fact]
    public void DuplicateGetsANewIdAndTraversalIsRejected()
    {
        WriteAsset("sprites/hero.sprite.json", "{}");
        using var database = CreateDatabase();
        var sourceId = Assert.Single(database.GetSnapshot().Assets).Id;

        Assert.True(database.TryDuplicate(
            "sprites/hero.sprite.json",
            out var duplicatePath,
            out var duplicateError), duplicateError);
        Assert.Equal("sprites/hero Copy.sprite.json", duplicatePath);
        Assert.True(database.TryGetAsset(duplicatePath!, out var duplicate));
        Assert.NotEqual(sourceId, duplicate!.Id);

        Assert.True(database.TryDelete(duplicatePath!, out var deleteError), deleteError);
        Assert.True(database.TryDuplicate(
            "sprites/hero.sprite.json",
            out var secondDuplicatePath,
            out var secondDuplicateError), secondDuplicateError);
        Assert.Equal("sprites/hero Copy 2.sprite.json", secondDuplicatePath);
        Assert.True(database.TryGetAsset(secondDuplicatePath!, out var secondDuplicate));
        Assert.NotEqual(duplicate.Id, secondDuplicate!.Id);

        Assert.False(database.TryCreateFolder("../outside", "bad", out var traversalError));
        Assert.Contains("cannot contain", traversalError, StringComparison.OrdinalIgnoreCase);
        Assert.False(Directory.Exists(Path.Combine(_root, "outside")));
    }

    [Fact]
    public void WatcherCoalescesAnExternalCreateIntoTheSnapshot()
    {
        using var database = CreateDatabase(enableWatcher: true);
        WriteAsset("external/new.scene.json", "{}");

        var observed = SpinWait.SpinUntil(() =>
        {
            Thread.Sleep(25);
            database.Update();
            return database.GetSnapshot().Assets.Any(asset =>
                asset.RelativePath == "external/new.scene.json");
        }, TimeSpan.FromSeconds(5));

        Assert.True(observed, "The external file was not observed by the filesystem watcher.");
    }

    [Fact]
    public void TaggedReferencesAreUnambiguousAndLegacyPathsStillSerialize()
    {
        var id = AssetId.New();
        var taggedJson = DreambitJson.Serialize(new AssetHolder
        {
            Asset = new TestAsset { AssetId = id, AssetName = "sprites/hero.sprite" }
        });
        var tagged = JObject.Parse(taggedJson)["Asset"]!;
        Assert.True(DreambitAssetReferenceToken.TryRead(tagged, out var parsedId, out var fallback));
        Assert.Equal(id, parsedId);
        Assert.Equal("sprites/hero.sprite", fallback);

        var legacyJson = DreambitJson.Serialize(new AssetHolder
        {
            Asset = new TestAsset { AssetName = "sprites/legacy.sprite" }
        });
        Assert.Equal("sprites/legacy.sprite", JObject.Parse(legacyJson).Value<string>("Asset"));
    }

    private AssetDatabase CreateDatabase(bool enableWatcher = false) =>
        new(_root, ContentRoot, enableWatcher: enableWatcher);

    private void WriteAsset(string relativePath, string contents)
    {
        var path = Path.Combine(ContentRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, contents);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, true);
    }

    private sealed class AssetHolder
    {
        public TestAsset? Asset { get; set; }
    }

    private sealed class TestAsset : DreambitAsset;
}
