using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Dreambit.LDtk;
using Newtonsoft.Json;

namespace Dreambit;

public class SceneBlueprint : DreambitAsset
{
    [JsonProperty("name", Required =  Required.Always)]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("entities")] public List<EntityBlueprint> Entities { get; set; } = [];

    [JsonProperty("ldtk")]
    public LDtkSceneReference LDtk { get; set; }
}
