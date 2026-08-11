using Dreambit.ECS;
using Dreambit.Editor.Inspection;
using Dreambit.Editor.Assets;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dreambit.Editor.Tests;

public sealed class InspectorMetadataTests
{
    [Fact]
    public void ComponentMetadataOnlyExposesDreambitSerializeMembers()
    {
        var cache = new InspectorMetadataCache();
        var members = cache.Get(typeof(InspectorTestComponent), InspectorTargetKind.Component);

        var speed = Assert.Single(members, member => member.SerializedName == nameof(InspectorTestComponent.Speed));
        Assert.Equal(0, speed.Range!.Minimum);
        Assert.Equal(10, speed.Range.Maximum);
        Assert.DoesNotContain(members, member => member.SerializedName == nameof(InspectorTestComponent.RuntimeCounter));
        Assert.DoesNotContain(members, member => member.SerializedName == nameof(InspectorTestComponent.Hidden));
    }

    [Fact]
    public void AssetMetadataUsesJsonContractInsteadOfDreambitSerialize()
    {
        var cache = new InspectorMetadataCache();
        var members = cache.Get(typeof(InspectorTestAsset), InspectorTargetKind.Asset);

        Assert.Contains(members, member => member.SerializedName == "title");
        Assert.Contains(members, member => member.SerializedName == nameof(InspectorTestAsset.Count));
        Assert.DoesNotContain(members, member => member.SerializedName == nameof(InspectorTestAsset.Ignored));
    }

    [Fact]
    public void AssetDocumentCaptureDoesNotWalkRuntimeOnlyObjectGraphs()
    {
        var path = Path.Combine(Path.GetTempPath(), $"circular-runtime-{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(path, "{\"editable\":1}");
            var file = new FileInfo(path);
            var record = new AssetRecord(
                AssetId.New(),
                file.Name,
                file.Name,
                string.Empty,
                Path.GetFileNameWithoutExtension(file.Name),
                AssetKind.Json,
                typeof(InspectorCircularRuntimeAsset).FullName,
                file.Length,
                file.LastWriteTimeUtc);
            using var document = DreambitAssetDocument.Open(
                record,
                path,
                typeof(InspectorCircularRuntimeAsset),
                new InspectorMetadataCache());

            document.Apply("Change editable value", asset =>
                ((InspectorCircularRuntimeAsset)asset).Editable = 2);
            var json = JObject.Parse(document.CaptureJson());

            Assert.Equal(2, json.Value<int>("editable"));
            Assert.Null(json[nameof(InspectorCircularRuntimeAsset.RuntimeCycle)]);
            var preview = new InspectorCircularRuntimeAsset();
            document.CopyInspectableValuesTo(preview);
            Assert.Equal(2, preview.Editable);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void AnimationAndSpriteSheetAssetsUseRecognizableSemanticSuffixes()
    {
        Assert.Equal(".animation.json", AssetTypeClassifier.GetFileSuffix(typeof(SpriteSheetAnimation)));
        Assert.Equal(".spritesheet.json", AssetTypeClassifier.GetFileSuffix(typeof(SpriteSheet)));

        var draft = new SpriteSheetAnimation();
        var restored = DreambitJson.Deserialize<SpriteSheetAnimation>(DreambitJson.Serialize(draft));
        Assert.NotNull(restored);
        Assert.Contains("sprite_sheet is required.", restored.GetValidationErrors());

        var spriteSheetMembers = new InspectorMetadataCache().Get(
            typeof(SpriteSheet),
            InspectorTargetKind.Asset);
        Assert.False(Assert.Single(spriteSheetMembers, member => member.SerializedName == "columns").IsReadOnly);
        Assert.False(Assert.Single(spriteSheetMembers, member => member.SerializedName == "rows").IsReadOnly);
    }

    [Fact]
    public void NewlyCreatedSpriteCanBeOpenedAsAnEditableDraft()
    {
        var path = Path.Combine(Path.GetTempPath(), $"new-sprite-{Guid.NewGuid():N}.sprite.json");
        try
        {
            var json = DreambitJson.Serialize(new Sprite());
            File.WriteAllText(path, json);
            var file = new FileInfo(path);
            var record = new AssetRecord(
                AssetId.New(),
                file.Name,
                file.Name,
                string.Empty,
                Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(file.Name)),
                AssetKind.Sprite,
                typeof(Sprite).FullName,
                file.Length,
                file.LastWriteTimeUtc);

            using var document = DreambitAssetDocument.Open(
                record,
                path,
                typeof(Sprite),
                new InspectorMetadataCache());

            var sprite = Assert.IsType<Sprite>(document.Instance);
            Assert.Equal(string.Empty, sprite.TexturePath);
            Assert.Null(sprite.Texture);
            Assert.DoesNotContain("Texture", JObject.Parse(document.CaptureJson()).Properties()
                .Select(property => property.Name));
        }
        finally
        {
            File.Delete(path);
        }
    }
}

public sealed class InspectorTestComponent : Component
{
    [DreambitSerialize, Dreambit.Range(0, 10)] public float Speed { get; set; }
    [DreambitSerialize, HideInInspector] public int Hidden { get; set; }
    public int RuntimeCounter { get; set; }
}

public sealed class InspectorTestAsset : DreambitAsset
{
    [JsonProperty("title")] public string Title { get; set; } = string.Empty;
    public int Count { get; set; }
    [JsonIgnore] public string Ignored { get; set; } = string.Empty;
}

public sealed class InspectorCircularRuntimeAsset : DreambitAsset
{
    [JsonProperty("editable")] public int Editable { get; set; }
    public InspectorCircularRuntimeAsset RuntimeCycle => this;
}
