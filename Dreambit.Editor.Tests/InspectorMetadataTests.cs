using Dreambit.ECS;
using Dreambit.Editor.Inspection;
using Dreambit.Editor.Assets;
using Dreambit.Editor.UI.Panels;
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
    public void SpriteDrawerExposesSpriteAssetReferenceInsteadOfSpritePath()
    {
        var members = new InspectorMetadataCache().Get(
            typeof(SpriteDrawer),
            InspectorTargetKind.Component);

        var sprite = Assert.Single(
            members,
            member => member.SerializedName == nameof(SpriteDrawer.Sprite));
        Assert.Equal(typeof(Sprite), sprite.ValueType);
        Assert.DoesNotContain(
            members,
            member => member.SerializedName == "SpritePath");
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
            Assert.Null(sprite.TextureAsset);
            Assert.Null(sprite.Texture);
            Assert.DoesNotContain("Texture", JObject.Parse(document.CaptureJson()).Properties()
                .Select(property => property.Name));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void BlueprintComponentReferencePickerOnlyOffersCompatibleSourceEntities()
    {
        var root = new EntityBlueprint
        {
            Name = "Player",
            Guid = Guid.NewGuid(),
            Components =
            [
                new ComponentBlueprint { Type = nameof(FilledRectDrawer) }
            ],
            Children =
            [
                new EntityBlueprint
                {
                    Name = "Child Without Mover",
                    Guid = Guid.NewGuid()
                }
            ]
        };

        var componentCandidates = InspectorPanel.GetBlueprintReferenceCandidates(
            root,
            typeof(FilledRectDrawer));
        Assert.Equal(root.Guid, Assert.Single(componentCandidates).Guid);

        var entityCandidates = InspectorPanel.GetBlueprintReferenceCandidates(root, typeof(Entity));
        Assert.Equal(2, entityCandidates.Count);
    }

    [Fact]
    public void AssetPickerCompatibilityUsesTheRequestedDreambitAssetType()
    {
        var animation = CreateAssetRecord(
            "characters/hero.animation.json",
            AssetKind.Animation,
            typeof(SpriteSheetAnimation));
        var blueprint = CreateAssetRecord(
            "characters/hero.blueprint.json",
            AssetKind.Blueprint,
            typeof(EntityBlueprint));
        var texture = CreateAssetRecord(
            "characters/hero.texture.png",
            AssetKind.Texture,
            typeof(TextureAsset));

        Assert.True(AssetTypeClassifier.IsCompatibleWith(animation, typeof(SpriteSheetAnimation)));
        Assert.False(AssetTypeClassifier.IsCompatibleWith(blueprint, typeof(SpriteSheetAnimation)));
        Assert.True(AssetTypeClassifier.IsCompatibleWith(texture, typeof(TextureAsset)));
        Assert.False(AssetTypeClassifier.IsCompatibleWith(texture, typeof(SpriteSheetAnimation)));
    }

    [Fact]
    public void BlueprintValidatorAcceptsTaggedAssetReferencesAndRejectsInlineCopies()
    {
        var component = new ComponentBlueprint
        {
            Type = typeof(InspectorAssetReferenceComponent).FullName!,
            Properties = new Dictionary<string, JToken>
            {
                [nameof(InspectorAssetReferenceComponent.Animation)] =
                    DreambitAssetReferenceToken.Create(
                        AssetId.New(),
                        "characters/hero.animation")
            }
        };
        var root = new EntityBlueprint
        {
            Name = "Player",
            Guid = Guid.NewGuid(),
            Components = [component]
        };

        Assert.Empty(BlueprintValidator.Validate(root));

        component.Properties[nameof(InspectorAssetReferenceComponent.Animation)] =
            JObject.FromObject(new { frames = Array.Empty<int>() });
        Assert.Contains(
            BlueprintValidator.Validate(root),
            error => error.Contains("asset references must be", StringComparison.Ordinal));
    }

    private static AssetRecord CreateAssetRecord(
        string relativePath,
        AssetKind kind,
        Type type) =>
        new(
            AssetId.New(),
            relativePath,
            Path.GetFileName(relativePath),
            Path.GetDirectoryName(relativePath)?.Replace('\\', '/') ?? string.Empty,
            Path.ChangeExtension(relativePath, null)!.Replace('\\', '/'),
            kind,
            type.FullName,
            0,
            DateTimeOffset.UtcNow);
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

public sealed class InspectorAssetReferenceComponent : Component
{
    [DreambitSerialize]
    public SpriteSheetAnimation? Animation { get; set; }
}
