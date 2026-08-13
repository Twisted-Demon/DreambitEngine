using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dreambit.LDtk;

/// <summary>
/// Persistent link from a Dreambit scene to an LDtk project/world. LDtk-owned
/// tile entities are regenerated when the scene is loaded; regular Dreambit
/// entities remain authored in the scene blueprint.
/// </summary>
public sealed class LDtkSceneReference
{
    [JsonProperty("asset_id")]
    public Guid AssetId { get; set; }

    [JsonProperty("asset", Required = Required.Always)]
    public string AssetName { get; set; } = string.Empty;

    [JsonProperty("world_iid")]
    public Guid WorldIid { get; set; }

    [JsonProperty("import_options")]
    public LDtkImportOptions ImportOptions { get; set; } = new();

    /// <summary>
    /// Editor-authored changes to generated LDtk visualization entities, keyed by a stable
    /// LDtk source identity. Fresh tile data is imported first and these values are applied last.
    /// </summary>
    [JsonProperty("entity_overrides")]
    public Dictionary<string, LDtkGeneratedEntityOverride> EntityOverrides { get; set; } =
        new(StringComparer.Ordinal);

    /// <summary>Compatibility facade for scenes written before import_options was introduced.</summary>
    [JsonIgnore]
    public float PixelsPerUnit
    {
        get => ImportOptions?.PixelsPerUnit ?? 1f;
        set
        {
            ImportOptions ??= new LDtkImportOptions();
            ImportOptions.PixelsPerUnit = value;
        }
    }

    // Newtonsoft uses this setter to read the original V1 scene format, but does not write it.
    [JsonProperty("pixels_per_unit")]
    private float LegacyPixelsPerUnit
    {
        set => PixelsPerUnit = value;
    }
}

/// <summary>Persistent Dreambit-side edits layered over a regenerated LDtk entity.</summary>
public sealed class LDtkGeneratedEntityOverride
{
    [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
    public string Name { get; set; }

    [JsonProperty("enabled", NullValueHandling = NullValueHandling.Ignore)]
    public bool? Enabled { get; set; }

    [JsonProperty("tags", NullValueHandling = NullValueHandling.Ignore)]
    public HashSet<string> Tags { get; set; }

    [JsonProperty("position", NullValueHandling = NullValueHandling.Ignore)]
    [JsonConverter(typeof(Vector3Converter))]
    public Vector3? Position { get; set; }

    [JsonProperty("rotation_2d", NullValueHandling = NullValueHandling.Ignore)]
    public float? Rotation2D { get; set; }

    [JsonProperty("scale", NullValueHandling = NullValueHandling.Ignore)]
    [JsonConverter(typeof(Vector3Converter))]
    public Vector3? Scale { get; set; }

    [JsonProperty("components")]
    public Dictionary<string, Dictionary<string, JToken>> Components { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);
}
