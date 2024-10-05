using Newtonsoft.Json;

namespace PixelariaEngine;

public class PixelariaAsset : DisposableObject
{
    [JsonIgnore]
    public string AssetName { get; set; }
}