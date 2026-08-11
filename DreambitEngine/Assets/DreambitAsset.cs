using Newtonsoft.Json;

namespace Dreambit;

public abstract class DreambitAsset : DisposableObject
{
    /// <summary>
    /// Stable project identity for this asset. Runtime-only assets may leave this empty.
    /// </summary>
    [JsonIgnore] public AssetId AssetId { get; set; }

    [JsonIgnore] public string AssetName { get; set; }
}
