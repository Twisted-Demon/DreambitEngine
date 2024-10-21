using Newtonsoft.Json;

namespace Dreambit;

public abstract class PixelariaAsset : DisposableObject
{
    [JsonIgnore] public string AssetName { get; set; }
}