using System;
using Newtonsoft.Json;

namespace Dreambit;

/// <summary>
/// Stable reference retained by a boxed Blueprint instance in a scene.
/// </summary>
public sealed class BlueprintInstanceReference
{
    /// <summary>Stable source asset identity. Empty for legacy/path-only projects.</summary>
    [JsonProperty("asset_id", DefaultValueHandling = DefaultValueHandling.Ignore)]
    public Guid AssetId { get; set; }

    /// <summary>Logical runtime asset name, retained as a readable fallback.</summary>
    [JsonProperty("asset", Required = Required.Always)]
    public string AssetName { get; set; } = string.Empty;
}
