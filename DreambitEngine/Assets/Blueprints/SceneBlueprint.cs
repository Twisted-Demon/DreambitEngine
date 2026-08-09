using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace Dreambit;

public class SceneBlueprint
{
    [JsonProperty("name", Required =  Required.Always)]
    public string Name { get; set; } = string.Empty;


}
