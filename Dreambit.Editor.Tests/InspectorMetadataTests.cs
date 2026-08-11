using Dreambit.ECS;
using Dreambit.Editor.Inspection;
using Newtonsoft.Json;

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
