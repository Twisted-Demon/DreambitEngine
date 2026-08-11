using System.Text.Json.Serialization;

namespace Dreambit.Editor.Assets;

internal sealed class AssetRegistryDocument
{
    public const int CurrentSchemaVersion = 1;
    public const string RelativePath = ".dreambit/assets.json";

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;
    public List<AssetRegistryEntry> Assets { get; set; } = [];
}

internal sealed class AssetRegistryEntry
{
    public Guid Id { get; set; }
    public string Path { get; set; } = string.Empty;
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AssetKind Kind { get; set; }
    public string? Type { get; set; }
    public long Length { get; set; }
    [JsonIgnore]
    public long LastWriteUtcTicks { get; set; }
    public string ContentHash { get; set; } = string.Empty;
}
