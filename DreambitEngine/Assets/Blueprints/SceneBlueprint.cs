using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Dreambit.LDtk;
using Dreambit.Tiled;
using Dreambit.ECS;
using Newtonsoft.Json;

namespace Dreambit;

[DreambitAssetType(
    "dreambit.blueprint.scene",
    FileExtension = DreambitAssetFileExtensions.SceneBlueprint)]
public class SceneBlueprint : DreambitAsset
{
    [DreambitSerialize]
    [JsonProperty("name", Required =  Required.Always)]
    public string Name { get; set; } = string.Empty;

    [DreambitSerialize]
    [JsonProperty("entities")] public List<EntityBlueprint> Entities { get; set; } = [];

    [DreambitSerialize]
    [JsonProperty("ldtk")]
    public LDtkSceneReference LDtk { get; set; }

    [DreambitSerialize]
    [JsonProperty("tiled")]
    public TiledSceneReference Tiled { get; set; }

    [DreambitSerialize]
    [JsonProperty("settings")]
    public SceneSettings Settings { get; set; } = new();
}
