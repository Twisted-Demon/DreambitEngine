using Newtonsoft.Json;

namespace PixelariaEngine;

public abstract class PixelariaAsset : DisposableObject
{
    [JsonIgnore] public string AssetName { get; set; }
}