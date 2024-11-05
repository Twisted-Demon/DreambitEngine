using Newtonsoft.Json;

namespace Dreambit;

public abstract class DreambitAsset : DisposableObject
{
    [JsonIgnore] public string AssetName { get; set; }
}